using System.Security.Cryptography;
using System.Text;

namespace THSR.Api.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string plainText)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash); // .NET 5+，會變成 64 字元十六進位字串
        }

        public static bool VerifyPassword(string plainText, string hash)
        {
            var hashedInput = HashPassword(plainText);
            return StringComparer.OrdinalIgnoreCase.Equals(hashedInput, hash);
        }
    }
}
