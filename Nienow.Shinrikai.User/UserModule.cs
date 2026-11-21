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
using Nienow.Shinrikai.Core.Adaptive;

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

    /// <summary>
    /// Dados que o próprio usuário pode alterar. Login e tipo (USR_TYPE) ficam de fora de propósito:
    /// o login identifica o usuário no token e o tipo só pode ser mudado pelo administrador.
    /// A senha nova é opcional; quando informada, exige a senha atual.
    /// </summary>
    public sealed class TProfileUpdate
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public ESex Sex { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Profession { get; set; }
        public string? OtherLanguages { get; set; }
        public EJLPT JLPT { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? RepeatPassword { get; set; }

        public bool ChangesPassword => !string.IsNullOrEmpty(NewPassword) || !string.IsNullOrEmpty(RepeatPassword);
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
        Task UpdateProfileAsync(int userId, TProfileUpdate profile, CancellationToken ct = default);
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

        /// <summary>
        /// O login é comparado sem diferenciar maiúsculas/minúsculas (UPPER nos dois lados); o usuário
        /// retornado traz o login como está gravado, que é o que vai para o token.
        /// </summary>
        private const string WhereLogin = " WHERE UPPER(USR_LOGIN) = UPPER(@LGN)";

        public Task<TUser?> GetByLoginAsync(string login, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync(SelectUser + WhereLogin, Map, ct, ("@LGN", login));

        public Task<TUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync(SelectUser + " WHERE USR_EMAIL = @EML", Map, ct, ("@EML", email));

        public async Task<string?> GetStoredPasswordAsync(string login, CancellationToken ct = default) =>
            await db.ScalarAsync("SELECT USR_PASS FROM USERS" + WhereLogin, ct, ("@LGN", login)) as string;

        public Task SignUpAsync(TUser user, CancellationToken ct = default) =>
            db.ExecuteAsync(
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

        /// <summary>Não altera USR_LOGIN nem USR_TYPE; USR_PASS só entra no UPDATE quando há senha nova.</summary>
        public Task UpdateProfileAsync(int userId, TProfileUpdate profile, CancellationToken ct = default)
        {
            var parameters = new List<(string Name, object? Value)>
            {
                ("@USR", profile.Name),
                ("@EML", profile.Email),
                ("@SEX", profile.Sex == ESex.Male ? "M" : "F"),
                ("@DAT", profile.Birthday),
                ("@JOB", profile.Profession),
                ("@LNG", profile.OtherLanguages),
                ("@JLPT", profile.JLPT == EJLPT.None ? null : profile.JLPT.ToString()),
                ("@ID", userId),
            };
            if (profile.ChangesPassword)
                parameters.Add(("@PSS", profile.NewPassword));

            return db.ExecuteAsync(
                $"""
                UPDATE USERS SET
                  USR_NAME = @USR, USR_EMAIL = @EML, USR_SEX = @SEX, USR_BIRTHDAY = @DAT,
                  USR_PROFISSION = @JOB, USR_OTHERLANGUAGES = @LNG, USR_JLPT = @JLPT{(profile.ChangesPassword ? ", USR_PASS = @PSS" : "")}
                WHERE USRID = @ID
                """,
                ct, [.. parameters]);
        }

        public async Task<string> GetNotificationAsync(int userId, int notificationId, bool force, CancellationToken ct = default) =>
            await db.ScalarAsync(
                $"""
                SELECT FIRST 1 NTF_MESSAGE FROM NOTIFICATIONS
                  LEFT OUTER JOIN NOT_STATUS ON NTF_ID = NST_NTF_ID AND NST_USR_ID = @USR
                WHERE NTF_ID = @NTF{(force ? "" : " AND (NST_UNDERSTAND IS NULL OR NST_UNDERSTAND <> 'S')")}
                """,
                ct, ("@USR", userId), ("@NTF", notificationId)) as string ?? "";

        public Task DeclareUnderstandingAsync(int userId, int notificationId, CancellationToken ct = default) =>
            db.ExecuteAsync(
                """
                INSERT INTO NOT_STATUS (NST_ID, NST_USR_ID, NST_NTF_ID, NST_UNDERSTAND, NST_DATE)
                VALUES (GEN_ID(NOTSTATUS_ID_GEN, 1), @USR, @NTF, 'S', @DT)
                """,
                ct, ("@USR", userId), ("@NTF", notificationId), ("@DT", DateTime.Now));

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
            JLPT = JlptLevel.Parse(r.GetNullableString("USR_JLPT")),
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
            var ok = stored is not null && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(stored.TrimEnd()),
                Encoding.UTF8.GetBytes(password));

            return ok ? await repository.GetByLoginAsync(login, ct) : null;
        }

        public async Task<Response> CheckAvailabilityAsync(string login, string? email, CancellationToken ct = default)
        {
            var messages = new List<string>();
            if (await repository.GetByLoginAsync(login, ct) is not null)
                messages.Add($"O login \"{login}\" já esta em uso.");
            if (!string.IsNullOrEmpty(email) && await repository.GetByEmailAsync(email, ct) is not null)
                messages.Add($"O email \"{email}\" já esta em uso.");

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
            if (user.RepeatPassword != user.Password)
                return Response.Fail("A senha repetida não é a mesma de senha informada.");

            return Response.Ok();
        }

        /// <summary>Validações da edição de perfil que não dependem do banco.</summary>
        public static Response ValidateProfile(TProfileUpdate profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
                return Response.Fail("Nome de usuário inválido.");
            if (string.IsNullOrEmpty(profile.Email) || !EmailRegex().IsMatch(profile.Email))
                return Response.Fail("Email inválido.");
            if (!profile.ChangesPassword)
                return Response.Ok();
            if (string.IsNullOrEmpty(profile.CurrentPassword))
                return Response.Fail("Informe a senha atual para trocar a senha.");
            if (profile.NewPassword is not { Length: >= MinPasswordLength })
                return Response.Fail($"A senha deve ter pelo menos {MinPasswordLength} dígitos.");
            if (profile.RepeatPassword != profile.NewPassword)
                return Response.Fail("A senha repetida não é a mesma de senha informada.");

            return Response.Ok();
        }

        /// <summary>Altera os dados do usuário <paramref name="login"/> (sempre o do token, nunca vindo do cliente).</summary>
        public async Task<Response> UpdateProfileAsync(string login, TProfileUpdate profile, CancellationToken ct = default)
        {
            profile.Name = profile.Name?.Trim();
            profile.Email = profile.Email?.Trim();

            var validation = ValidateProfile(profile);
            if (!validation.OK)
                return validation;

            if (await repository.GetByLoginAsync(login, ct) is not { } user)
                return Response.Fail("Usuário não encontrado.");

            if (await repository.GetByEmailAsync(profile.Email!, ct) is { } other && other.ID != user.ID)
                return Response.Fail($"O email \"{profile.Email}\" já esta em uso.");

            if (profile.ChangesPassword && await LoginAsync(login, profile.CurrentPassword!, ct) is null)
                return Response.Fail("Senha atual incorreta.");

            await repository.UpdateProfileAsync(user.ID, profile, ct);
            return Response.Ok("Dados alterados com sucesso!");
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
