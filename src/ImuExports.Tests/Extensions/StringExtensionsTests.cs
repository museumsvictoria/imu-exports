using ImuExports.Extensions;
using Shouldly;

namespace ImuExports.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void RemoveDiacritics_WithDiacritic_ReplacesDiacritic()
    {
        // Given
        var input = "Sérandite";

        // When
        var result = input.RemoveDiacritics();
        
        // Then
        result.ShouldBe("Serandite");
    }
}