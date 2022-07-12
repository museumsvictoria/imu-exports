using System.Text.RegularExpressions;

namespace ImuExports.Utilities;

public class LinkHeader
{
    public Uri FirstLink { get; set; }
    public Uri PrevLink { get; set; }
    public Uri NextLink { get; set; }
    public Uri LastLink { get; set; }

    public static LinkHeader LinksFromHeader(string linkHeaderParameter)
    {
        LinkHeader linkHeader = null;

        if (!string.IsNullOrWhiteSpace(linkHeaderParameter))
        {
            var linkStrings = linkHeaderParameter.Split(',');

            if (linkStrings.Any())
            {
                linkHeader = new LinkHeader();

                foreach (var linkString in linkStrings)
                {
                    var relMatch = Regex.Match(linkString, "(?<=rel=\").+?(?=\")", RegexOptions.IgnoreCase);
                    var linkMatch = Regex.Match(linkString, "(?<=<).+?(?=>)", RegexOptions.IgnoreCase);

                    if (relMatch.Success && linkMatch.Success && Uri.TryCreate(linkMatch.Value, UriKind.Absolute, out var link))
                    {
                        switch (relMatch.Value.ToLower())
                        {
                            case "first":
                                linkHeader.FirstLink = link;
                                break;
                            case "prev":
                                linkHeader.PrevLink = link;
                                break;
                            case "next":
                                linkHeader.NextLink = link;
                                break;
                            case "last":
                                linkHeader.LastLink = link;
                                break;
                        }
                    }
                }
            }
        }

        return linkHeader;
    }
}