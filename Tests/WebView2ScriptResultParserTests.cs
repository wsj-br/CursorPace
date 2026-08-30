using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class WebView2ScriptResultParserTests
{
    [Fact]
    public void TryParse_ObjectPayload_ReadsStatusAndBody()
    {
        Assert.True(WebView2ScriptResultParser.TryParse(
            """{"status":200,"body":"{\"ok\":true}"}""",
            out var status,
            out var body));

        Assert.Equal(200, status);
        Assert.Equal("""{"ok":true}""", body);
    }

    [Fact]
    public void TryParse_JsonEncodedStringPayload_ReadsStatusAndBody()
    {
        var encoded = JsonQuote("""{"status":401,"body":"Sign in"}""");

        Assert.True(WebView2ScriptResultParser.TryParse(encoded, out var status, out var body));
        Assert.Equal(401, status);
        Assert.Equal("Sign in", body);
    }

    [Fact]
    public void TryParse_EmptyObject_ReturnsFalse()
    {
        Assert.False(WebView2ScriptResultParser.TryParse("{}", out var status, out var body));
        Assert.Equal(0, status);
        Assert.Equal(string.Empty, body);
    }

    [Fact]
    public void TryParse_NullToken_ReturnsFalse()
    {
        Assert.False(WebView2ScriptResultParser.TryParse("null", out _, out _));
    }

    [Fact]
    public void TryParse_WebKitStyleStringifiedPayload_ReadsStatusAndBody()
    {
        // Linux/macOS InvokeScript receives the JS string from JSON.stringify(payload).
        Assert.True(WebView2ScriptResultParser.TryParse(
            """{"status":200,"body":"{\"billingCycleStart\":\"2026-01-01T00:00:00.000Z\"}"}""",
            out var status,
            out var body));

        Assert.Equal(200, status);
        Assert.Equal("""{"billingCycleStart":"2026-01-01T00:00:00.000Z"}""", body);
    }

    private static string JsonQuote(string value) =>
        System.Text.Json.JsonSerializer.Serialize(value);
}
