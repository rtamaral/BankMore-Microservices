using System.Security.Cryptography;
using System.Text;

namespace BankMore.Api.Application.Services
{
    public interface IPasswordService
    {
        (string Hash, string Salt) HashPassword(string password);
        bool VerifyPassword(string password, string hash, string salt);
    }

    public class PasswordService : IPasswordService
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            string salt = Convert.ToBase64String(saltBytes);

            using var sha256 = SHA256.Create();
            byte[] combined = Encoding.UTF8.GetBytes(password + salt);
            string hash = Convert.ToBase64String(sha256.ComputeHash(combined));

            return (hash, salt);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            using var sha256 = SHA256.Create();
            byte[] combined = Encoding.UTF8.GetBytes(password + salt);
            string computedHash = Convert.ToBase64String(sha256.ComputeHash(combined));

            return computedHash == hash;
        }
    }
}
