using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ImuExports.Extensions;

public static class StringExtensions
{
    public static string Concatenate(this IEnumerable<string> input, string delimiter)
    {
        var result = new StringBuilder();

        if (input != null)
            foreach (var item in input)
                if (!string.IsNullOrWhiteSpace(item))
                {
                    if (result.Length != 0)
                        result.Append(delimiter);

                    result.Append(item);
                }

        return result.Length != 0 ? result.ToString() : null;
    }

    public static string ReplaceLineBreaks(this string input, string delimiter = " ")
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex
            .Replace(input, @"\r\n?|\n", delimiter)
            .Trim();
    }

    public static string RemoveNonWordCharacters(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex.Replace(input, @"[^\w\s]", string.Empty);
    }
    
    public static string RemoveDiacritics(this string input) 
    {
        if (input is null)
            return null;
        
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalizedString.EnumerateRunes())
        {
            var unicodeCategory = Rune.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}