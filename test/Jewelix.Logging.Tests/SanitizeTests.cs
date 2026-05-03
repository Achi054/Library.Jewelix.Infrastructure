namespace Jewelix.Logging.Tests;

/// <summary>
/// Tests for <see cref="LoggerMiddleware.Sanitize"/>: empty-input handling, masking of
/// known sensitive JSON fields (password, token, secret, creditCard, apiKey),
/// case-insensitive matching, preservation of non-sensitive fields, and truncation of
/// payloads that exceed the maximum logging length.
/// </summary>
public class SanitizeTests
{
    [Fact]
    public void Sanitize_WithEmptyInput_ReturnsEmpty()
    {
        LoggerMiddleware.Sanitize(string.Empty).ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("""{"password":"hunter2","name":"alice"}""", """{"password":"***MASKED***","name":"alice"}""")]
    [InlineData("""{"token": "abc.def.ghi"}""", """{"token": "***MASKED***"}""")]
    [InlineData("""{"creditCard":"4111-1111-1111-1111"}""", """{"creditCard":"***MASKED***"}""")]
    [InlineData("""{"apiKey":"sk_live_xyz"}""", """{"apiKey":"***MASKED***"}""")]
    [InlineData("""{"PASSWORD":"upper"}""", """{"PASSWORD":"***MASKED***"}""")]
    public void Sanitize_WithKnownSensitiveFields_MasksValues(string input, string expected)
    {
        LoggerMiddleware.Sanitize(input).ShouldBe(expected);
    }

    [Fact]
    public void Sanitize_WithNonSensitiveFields_LeavesFieldsUntouched()
    {
        const string input = """{"name":"alice","email":"alice@example.com"}""";
        LoggerMiddleware.Sanitize(input).ShouldBe(input);
    }

    [Fact]
    public void Sanitize_WithInputExceedingMaxLength_TruncatesAndAppendsSuffix()
    {
        var input = new string('a', 5_000);
        var result = LoggerMiddleware.Sanitize(input);
        result.Length.ShouldBeLessThan(input.Length);
        result.ShouldEndWith("...(truncated)");
    }
}
