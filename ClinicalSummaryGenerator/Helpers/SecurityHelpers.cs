namespace ClinicalSummaryGenerator.Helpers;

public static class SecurityHelpers
{
    public static bool IsAuthorized(HttpRequest request, IConfiguration config, out string? error)
    {
        error = null;

        var expectedToken = config["Security:ApiKey"];
        var authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            error = "Missing or invalid Authorization header.";
            return false;
        }

        var actualToken = authHeader["Bearer ".Length..].Trim();

        if (!string.Equals(actualToken, expectedToken))
        {
            error = "Invalid token.";
            return false;
        }

        return true;
    }
}
