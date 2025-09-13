using System.Security.Cryptography;
using System.Text;

namespace InstrunetBackend.Server;

internal static class InstrunetExtensions
{
    public static string Sha256HexHashString(this string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        var hex = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return hex;
    }

    public static byte[] DataUrlToByteArray(this string data)
    {
        string base64Data = data.Split(',')[1];
        var binData = Convert.FromBase64String(base64Data);
        return binData;
    }
    public static string ToPinyin(this string kanji)
    {
        return NPinyin.Pinyin.GetPinyin(kanji);
    }
}