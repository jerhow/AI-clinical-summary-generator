using ClinicalSummaryGenerator.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace ClinicalSummaryGenerator.Tests.Helpers;

[TestFixture]
public class SecurityHelpersTests
{
    private const string ValidToken = "test-token-123";
    private IConfiguration _config = default!;

    [SetUp]
    public void SetUp()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Security:ApiKey"] = ValidToken
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(dict!)
            .Build();
    }

    private static HttpRequest CreateRequestWithAuthHeader(string? headerValue)
    {
        var context = new DefaultHttpContext();
        if (headerValue != null)
        {
            context.Request.Headers["Authorization"] = headerValue;
        }
        return context.Request;
    }

    [Test]
    public void IsAuthorized_WithValidToken_ReturnsTrue()
    {
        var request = CreateRequestWithAuthHeader($"Bearer {ValidToken}");

        var result = SecurityHelpers.IsAuthorized(request, _config, out var error);

        Assert.IsTrue(result);
        Assert.IsNull(error);
    }

    [Test]
    public void IsAuthorized_WithInvalidToken_ReturnsFalse()
    {
        var request = CreateRequestWithAuthHeader("Bearer wrong-token");

        var result = SecurityHelpers.IsAuthorized(request, _config, out var error);

        Assert.IsFalse(result);
        Assert.AreEqual("Invalid token.", error);
    }

    [Test]
    public void IsAuthorized_WithMissingHeader_ReturnsFalse()
    {
        var request = CreateRequestWithAuthHeader(null);

        var result = SecurityHelpers.IsAuthorized(request, _config, out var error);

        Assert.IsFalse(result);
        Assert.AreEqual("Missing or invalid Authorization header.", error);
    }

    [Test]
    public void IsAuthorized_WithNonBearerHeader_ReturnsFalse()
    {
        var request = CreateRequestWithAuthHeader("Basic xyz123");

        var result = SecurityHelpers.IsAuthorized(request, _config, out var error);

        Assert.IsFalse(result);
        Assert.AreEqual("Missing or invalid Authorization header.", error);
    }
}
