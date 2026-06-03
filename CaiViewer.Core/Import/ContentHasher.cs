using System.Security.Cryptography;
using System.Text;

namespace CaiViewer.Core.Import;

public static class ContentHasher
{
    public static string Compute(string modelJson, string versionJson)
    {
        var combined = modelJson + versionJson;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexStringLower(bytes);
    }
}
