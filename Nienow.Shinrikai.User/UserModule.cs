using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;

namespace Nienow.Shinrikai.User
{
    public class TUser
    {
        public int ID { get; set; }
        public DateTime Birthday { get; set; }
        public string? Email { get; set; }
        public EJLPT JLPT { get; set; }
        public string? Login { get; set; }
        public string? Name { get; set; }
        public string? OtherLanguages { get; set; }
        public string? Password { get; set; }
        public string? Profession { get; set; }
        public string? RepeatPassword { get; set; }
        public ESex Sex { get; set; }
        /// <summary>USR_TYPE: "A" administrador, "T" professor, "S"/null aluno.</summary>
        public string? Type { get; set; }

        public string Role => Roles.FromUserType(Type);
    }

    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Teacher = "Teacher";
        public const string Student = "Student"; // original: "Studenty" (typo, nunca usado em [Authorize])
        public const string ContentEditors = Administrator + "," + Teacher;

        /// <summary>CustomRoleProvider.GetRolesForUser.</summary>
        public static string FromUserType(string? type) => type switch
        {
            "A" => Administrator,
            "T" => Teacher,
            _ => Student,
        };
    }

    public interface IUserRepository
    {
        Task<TUser?> GetByLoginAsync(string login, CancellationToken ct = default);
        Task<TUser?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<string?> GetStoredPasswordAsync(string login, CancellationToken ct = default);
        Task SignUpAsync(TUser user, CancellationToken ct = default);
        Task<string> GetNotificationAsync(int userId, int notificationId, bool force, CancellationToken ct = default);
        Task DeclareUnderstandingAsync(int userId, int notificationId, CancellationToken ct = default);
    }

    /// <summary>Port de DBUser.cs com SQL parametrizado.</summary>
    public sealed class UserRepository(IShinrikaiDb db) : IUserRepository
    {
        private const string SelectUser =
            """
            SELECT USRID, USR_NAME, USR_EMAIL, USR_LOGIN, USR_SEX, USR_BIRTHDAY,
              USR_PROFISSION, USR_OTHERLANGUAGES, USR_JLPT, USR_TYPE
            FROM USERS
            """;

        public async Task<TUser?> GetByLoginAsync(string login, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QuerySingleOrDefaultAsync(SelectUser + " WHERE USR_LOGIN = @LGN", Map, ct, ("@LGN", login));
        }

        public async Task<TUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QuerySingleOrDefaultAsync(SelectUser + " WHERE USR_EMAIL = @EML", Map, ct, ("@EML", email));
        }

        public async Task<string?> GetStoredPasswordAsync(string login, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.ScalarAsync("SELECT USR_PASS FROM USERS WHERE USR_LOGIN = @LGN", ct, ("@LGN", login)) as string;
        }

        public async Task SignUpAsync(TUser user, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            await conn.ExecuteInTransactionAsync(
                """
                INSERT INTO USERS (
                  USRID, USR_NAME, USR_EMAIL, USR_LOGIN, USR_PASS,
                  USR_SEX, USR_BIRTHDAY, USR_PROFISSION, USR_OTHERLANGUAGES, USR_JLPT
                ) VALUES (
                  GEN_ID(USERS_ID_GEN, 1), @USR, @EML, @LGN, @PSS,
                  @SEX, @DAT, @JOB, @LNG, @JLPT
                )
                """,
                ct,
                ("@USR", user.Name),
                ("@EML", user.Email),
                ("@LGN", user.Login),
                ("@PSS", user.Password),
                ("@SEX", user.Sex == ESex.Male ? "M" : "F"),
                ("@DAT", user.Birthday),
                ("@JOB", user.Profession),
                ("@LNG", user.OtherLanguages),
                ("@JLPT", user.JLPT == EJLPT.None ? null : user.JLPT.ToString()));
        }

        public async Task<string> GetNotificationAsync(int userId, int notificationId, bool force, CancellationToken ct = default)
        {
            var sql = new StringBuilder(
                """
                SELECT FIRST 1 NTF_MESSAGE FROM NOTIFICATIONS
                  LEFT OUTER JOIN NOT_STATUS ON NTF_ID = NST_NTF_ID AND NST_USR_ID = @USR
                WHERE NTF_ID = @NTF
                """);
            if (!force)
                sql.Append(" AND (NST_UNDERSTAND IS NULL OR NST_UNDERSTAND <> 'S')");

            await using var conn = await db.OpenAsync(ct);
            return await conn.ScalarAsync(sql.ToString(), ct, ("@USR", userId), ("@NTF", notificationId)) as string ?? "";
        }

        public async Task DeclareUnderstandingAsync(int userId, int notificationId, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            await conn.ExecuteInTransactionAsync(
                """
                INSERT INTO NOT_STATUS (NST_ID, NST_USR_ID, NST_NTF_ID, NST_UNDERSTAND, NST_DATE)
                VALUES (GEN_ID(NOTSTATUS_ID_GEN, 1), @USR, @NTF, 'S', @DT)
                """,
                ct, ("@USR", userId), ("@NTF", notificationId), ("@DT", DateTime.Now));
        }

        private static TUser Map(DbDataReader r) => new()
        {
            ID = r.GetInt("USRID"),
            Name = r.GetNullableString("USR_NAME"),
            Email = r.GetNullableString("USR_EMAIL"),
            Login = r.GetNullableString("USR_LOGIN"),
            Sex = r.GetNullableString("USR_SEX") == "M" ? ESex.Male : ESex.Female,
            Birthday = r.GetDateTimeOrDefault("USR_BIRTHDAY"),
            Profession = r.GetNullableString("USR_PROFISSION"),
            OtherLanguages = r.GetNullableString("USR_OTHERLANGUAGES"),
            JLPT = Enum.TryParse<EJLPT>(r.GetNullableString("USR_JLPT") ?? "None", true, out var lvl) ? lvl : EJLPT.None,
            Type = r.GetNullableString("USR_TYPE"),
        };
    }

    /// <summary>Port de UserManager.cs (sem estado de sessão: o usuário corrente vem do JWT).</summary>
    public sealed partial class UserService(IUserRepository repository)
    {
        public const int MinPasswordLength = 5;

        [GeneratedRegex(@"^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex EmailRegex();

        public Task<TUser?> GetUserAsync(string login, CancellationToken ct = default) => repository.GetByLoginAsync(login, ct);

        /// <summary>
        /// O original comparava a senha dentro do SQL (concatenada — SQL injection no login).
        /// Aqui a senha é lida por login parametrizado e comparada em tempo constante.
        /// A base legada guarda a senha em texto puro; ver README (hash requer ALTER TABLE).
        /// </summary>
        public async Task<TUser?> LoginAsync(string login, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                return null;

            var stored = await repository.GetStoredPasswordAsync(login, ct);
            if (stored is null)
                return null;

            var ok = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(stored.TrimEnd()),
                Encoding.UTF8.GetBytes(password));

            return ok ? await repository.GetByLoginAsync(login, ct) : null;
        }

        public async Task<Response> CheckAvailabilityAsync(string login, string? email, CancellationToken ct = default)
        {
            var freeLogin = await repository.GetByLoginAsync(login, ct) is null;
            var freeEmail = string.IsNullOrEmpty(email) || await repository.GetByEmailAsync(email, ct) is null;

            var messages = new List<string>();
            if (!freeLogin) messages.Add($"O login \"{login}\" já esta em uso.");
            if (!freeEmail) messages.Add($"O email \"{email}\" já esta em uso.");

            return messages.Count == 0 ? Response.Ok() : Response.Fail(string.Join(Environment.NewLine, messages));
        }

        public static Response Validate(TUser user)
        {
            if (string.IsNullOrEmpty(user.Name))
                return Response.Fail("Nome de usuário inválido.");
            if (string.IsNullOrEmpty(user.Email) || !EmailRegex().IsMatch(user.Email))
                return Response.Fail("Email inválido.");
            if (string.IsNullOrEmpty(user.Login))
                return Response.Fail("Login inválido.");
            if (string.IsNullOrEmpty(user.Password))
                return Response.Fail("Senha inválida.");
            // Regra que o original só aplicava no navegador (Views/Login/Index.cshtml).
            if (user.Password.Length < MinPasswordLength)
                return Response.Fail($"A senha deve ter pelo menos {MinPasswordLength} dígitos.");
            // O original só checava se RepeatPassword era vazio; a mensagem indica a intenção de comparar.
            if (string.IsNullOrEmpty(user.RepeatPassword) || user.RepeatPassword != user.Password)
                return Response.Fail("A senha repetida não é a mesma de senha informada.");

            return Response.Ok();
        }

        public async Task<Response> SignUpAsync(TUser user, CancellationToken ct = default)
        {
            var validation = Validate(user);
            if (!validation.OK)
                return validation;

            var availability = await CheckAvailabilityAsync(user.Login!, user.Email, ct);
            if (!availability.OK)
                return availability;

            await repository.SignUpAsync(user, ct);
            return Response.Ok("Usuário cadastrado com sucesso!");
        }
    }
}
