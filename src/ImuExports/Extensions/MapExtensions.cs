using System.Text;
using IMu;

namespace ImuExports.Extensions;

public static class MapExtensions
{
    public static string GetTrimString(this Map map, string input)
    {
        return TrimString(map.GetString(input));
    }

    public static IList<string> GetTrimStrings(this Map map, string input)
    {
        var mapStrings = map.GetStrings(input);

        if (mapStrings != null && mapStrings.Any())
            return mapStrings
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(TrimString)
                .ToList();

        return new List<string>();
    }

    public static string GetCleanString(this Map map, string input)
    {
        return map.GetString(input).RemoveNonWordCharacters();
    }
    
    public static string GetEncodedString(this Map map, string name)
    {
        return EncodeString(map.GetString(name));
    }

    private static string TrimString(string input)
    {
        return !string.IsNullOrWhiteSpace(input) ? input.Trim() : input;
    }
    
    private static string EncodeString(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return Encoding.GetEncoding("Windows-1252").GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(value)).Trim();

        return value;
    }
}