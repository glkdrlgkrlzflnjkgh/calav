using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CalavHashScanner.Utils
{
    public static class HashUtils
    {
        public static string ComputeSha256File(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(stream);
            return BytesToHex(hashBytes);
        }

        public static string ComputeSha256String(string text)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = sha.ComputeHash(bytes);
            return BytesToHex(hashBytes);
        }

        public static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
