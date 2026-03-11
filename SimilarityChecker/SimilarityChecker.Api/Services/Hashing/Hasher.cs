using System.Security.Cryptography;
using System.Text;

namespace SimilarityChecker.Api.Services;

public static class Hasher
{
    public static string Sha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
