using System;
using System.Security.Cryptography;
using System.Text;

namespace Entertainment.Security;

internal class PasswordSecurity
{
	public static string HashPassword(string password)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] bytes = Encoding.UTF8.GetBytes(password);
		return Convert.ToBase64String(sHA.ComputeHash(bytes));
	}

	public static bool VerifyPassword(string password, string storedHash)
	{
		string x = HashPassword(password);
		return StringComparer.Ordinal.Compare(x, storedHash) == 0;
	}
}
