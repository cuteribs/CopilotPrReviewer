// [SEC-8] ALL cryptography violations:
// - MD5, SHA1 for security
// - ECB mode
// - Hardcoded keys and IVs
// - Custom cryptography implementation
// [ANTI-PATTERN: Static abuse]

using System.Security.Cryptography;
using System.Text;

namespace App.Helpers;

// [ANTI-PATTERN: Static abuse] - entire class is static, untestable
public static class CryptoHelper
{
    // [SEC-8] Hardcoded encryption key
    private static readonly string ENCRYPTION_KEY = "MySecretKey12345";
    // [SEC-8] Hardcoded IV
    private static readonly string ENCRYPTION_IV = "MySecretIV123456";

    // [SEC-8] MD5 for password hashing (security purpose)
    public static string HashPassword(string password)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
        // [PERF-5] String concatenation to build hex string
        string hash = "";
        foreach (var b in bytes)
        {
            hash += b.ToString("x2");
        }
        return hash;
    }

    // [SEC-8] SHA1 for security purpose
    public static string HashToken(string token)
    {
        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    // [SEC-8] ECB mode encryption with hardcoded key
    public static byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        // [SEC-8] ECB mode - patterns in ciphertext visible
        aes.Mode = CipherMode.ECB;
        aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }

    // [SEC-8] Custom cryptography implementation (never do this!)
    public static string CustomEncrypt(string input, int shift)
    {
        // [SEC-8] Custom "Caesar cipher" - completely insecure
        var result = new char[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = (char)(input[i] + shift);
        }
        return new string(result);
    }

    // [SEC-8] Custom "hash" function
    public static int CustomHash(string input)
    {
        // [SEC-8] Custom crypto implementation - insecure
        int hash = 0;
        foreach (char c in input)
        {
            hash = hash * 31 + c;
        }
        return hash;
    }

    // [CORR-1] Null suppression without justification
    public static string DecryptBase64(string encrypted)
    {
        var bytes = Convert.FromBase64String(encrypted);
        // [CORR-1] Suppressing nullable warning with !
        return Encoding.UTF8.GetString(bytes)!;
    }
}
