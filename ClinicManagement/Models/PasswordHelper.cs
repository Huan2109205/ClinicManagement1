
using System;
using System.Security.Cryptography;

namespace ClinicManagement.Models
{
    public static class PasswordHelper
    {
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                return Iterations + "." +
                    Convert.ToBase64String(salt) + "." +
                    Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(
            string password,
            string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                string[] parts = storedHash.Split('.');

                if (parts.Length != 3)
                    return false;

                int iterations = int.Parse(parts[0]);

                if (iterations < 10000 || iterations > 1000000)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expectedHash = Convert.FromBase64String(parts[2]);

                if (salt.Length != 16 || expectedHash.Length != 32)
                    return false;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256))
                {
                    byte[] actualHash = pbkdf2.GetBytes(32);

                    int difference = 0;

                    for (int i = 0; i < actualHash.Length; i++)
                    {
                        difference |= actualHash[i] ^ expectedHash[i];
                    }

                    return difference == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
