using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Entertainment.Security
{
    class PasswordSecurity
    {
        //add password security features here, use hashpasswords
        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256Hash.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        public static bool VerifyPassword(string password, string storedHash)
        {
            string hashOfInput = HashPassword(password);
            return StringComparer.Ordinal.Compare(hashOfInput, storedHash) == 0;
        }
    }
}
