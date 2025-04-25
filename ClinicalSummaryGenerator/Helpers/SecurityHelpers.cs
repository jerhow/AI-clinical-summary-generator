namespace ClinicalSummaryGenerator.Helpers;

public static class SecurityHelpers
{
    /// <summary>
    /// Checks if the request is authorized by validating the API key in the Authorization header.
    /// It compares the token in the header with the expected token from the configuration.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="config"></param>
    /// <param name="error"></param>
    /// <returns></returns>
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
