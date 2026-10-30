using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CalavHashScanner.Util;

public static class HashUtils
{
    public static string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexStringLower(bytes);
    }
    public static string ComputeSha256File(string path)
    {
        using var stream = File.OpenRead(path);
        var hashBytes = SHA256.HashData(stream);
        return BytesToHex(hashBytes);
    }

    public static string ComputeSha256String(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hashBytes = SHA256.HashData(bytes);
        return BytesToHex(hashBytes);
    }
}
