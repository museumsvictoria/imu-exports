namespace ImuExports.Infrastructure;

public interface IWithTermFilter
{
    IList<KeyValuePair<string, string>> TermFilters { get; set; }
}