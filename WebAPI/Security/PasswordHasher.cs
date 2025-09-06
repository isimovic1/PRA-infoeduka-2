using System.Security.Cryptography;

namespace WebAPI.Security
{
    /// <summary>
    /// PBKDF2 (SHA256) password hasher.
    /// Stored format:  PBKDF2$<iterations>$<base64(salt)>$<base64(hash)>
    /// </summary>
    public static class PasswordHasher
    {
        // Keep these the same app-wide; you can raise Iterations later if needed.
        private const int Iterations = 100_000;
        private const int SaltSize = 16;  // 128-bit salt
        private const int KeySize = 32;  // 256-bit key

        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                var parts = stored.Split('$');
                if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

                var iterations = int.Parse(parts[1]);
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var actual = pbkdf2.GetBytes(expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }
    }
}
