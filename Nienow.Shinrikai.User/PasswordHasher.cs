using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Nienow.Shinrikai.User
{
    /// <summary>
    /// Hash de senha com PBKDF2-HMAC-SHA256 (Rfc2898DeriveBytes), sal aleatório por senha.
    /// Formato gravado em USERS.USR_PASS: <c>PBKDF2$SHA256$&lt;iterações&gt;$&lt;sal base64&gt;$&lt;hash base64&gt;</c>
    /// (cerca de 90 caracteres, cabe no VARCHAR(100)). As iterações vão junto com o hash para que
    /// possam ser aumentadas depois: hashes com menos iterações são refeitos no próximo login.
    /// Valores sem o prefixo são senhas em texto puro herdadas da base original.
    /// </summary>
    public static class PasswordHasher
    {
        public const string Prefix = "PBKDF2$SHA256$";

        /// <summary>Recomendação da OWASP (2023) para PBKDF2-HMAC-SHA256.</summary>
        public const int Iterations = 600_000;

        /// <summary>Tamanho mínimo de USR_PASS para guardar o hash.</summary>
        public const int ColumnLength = 100;

        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static bool IsHash(string? stored) => stored is not null && stored.StartsWith(Prefix, StringComparison.Ordinal);

        public static string Hash(string password) => Hash(password, Iterations);

        public static string Hash(string password, int iterations)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, Algorithm, HashBytes);
            return string.Join('$', Prefix.TrimEnd('$'), iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        /// <summary>
        /// Confere a senha contra o valor gravado, seja hash ou texto puro herdado (comparação em tempo
        /// constante nos dois casos). <paramref name="needsRehash"/> indica que, com a senha correta, o
        /// valor gravado deve ser trocado por um hash novo (texto puro ou iterações abaixo do atual).
        /// </summary>
        public static bool Verify(string password, string? stored, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
                return false;

            if (!IsHash(stored))
            {
                // A base original pode ter CHAR com espaços à direita.
                var ok = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(stored.TrimEnd()), Encoding.UTF8.GetBytes(password));
                needsRehash = ok;
                return ok;
            }

            if (!TryParse(stored, out var iterations, out var salt, out var expected))
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, Algorithm, expected.Length);
            var match = CryptographicOperations.FixedTimeEquals(actual, expected);
            needsRehash = match && iterations < Iterations;
            return match;
        }

        private static bool TryParse(string stored, out int iterations, out byte[] salt, out byte[] hash)
        {
            iterations = 0;
            salt = hash = [];
            var parts = stored[Prefix.Length..].Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations <= 0)
                return false;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                hash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }
            return salt.Length > 0 && hash.Length > 0;
        }
    }
}
