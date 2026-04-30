using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using InstrunetBackend.Server.InstrunetModels;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NPinyin;

namespace InstrunetBackend.Server;

internal static class InstrunetExtensions
{
    public static Expression<Func<InstrunetEntry, bool>> GetChineseSearchPredicate(string search)
    {
        return i =>
            // i.SongName.Contains(search) ||
            // i.AlbumName.Contains(search) ||
            // i.Artist!.Contains(search) ||
            // i.SongName.Contains(ChineseConverter.Convert(search,
            //     ChineseConversionDirection.SimplifiedToTraditional)) ||
            // i.AlbumName.Contains(ChineseConverter.Convert(search,
            //     ChineseConversionDirection.SimplifiedToTraditional)) ||
            // i.Artist!.Contains(ChineseConverter.Convert(search,
            //     ChineseConversionDirection.SimplifiedToTraditional)) ||
            // i.SongName.Contains(ChineseConverter.Convert(search,
            //     ChineseConversionDirection.TraditionalToSimplified)) ||
            // i.AlbumName.Contains(ChineseConverter.Convert(search,
            //     ChineseConversionDirection.TraditionalToSimplified)) ||
            // i.Artist!.Contains(ChineseConverter.Convert(search,
            //     ChineseConversionDirection.TraditionalToSimplified));
            GetChineseSearchPredicateGeneral(i.SongName, search) || 
            GetChineseSearchPredicateGeneral(i.AlbumName, search) || 
            GetChineseSearchPredicateGeneral(i.Artist?? "", search);
    }

    public static bool GetChineseSearchPredicateGeneral(string source, string search)
    {
        return source.Contains(search) || source.Contains(ChineseConverter.Convert(search,
            ChineseConversionDirection.SimplifiedToTraditional)) || source.Contains(ChineseConverter.Convert(search,
            ChineseConversionDirection.TraditionalToSimplified)) ;
    }

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
        var base64Data = data.Split(',')[1];
        var binData = Convert.FromBase64String(base64Data);
        return binData;
    }

    public static string ToPinyin(this string kanji)
    {
        return Pinyin.GetPinyin(kanji);
    }

    // Bruh. 
    public static void PrintLn(this string s)
    {
        Console.WriteLine(s);
    }
}