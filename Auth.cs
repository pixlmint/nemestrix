using System.Security.Cryptography;
using System.Text;

namespace Pixlmint.Nemestrix.Auth;

public static class ApiKeyAuthenticationExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Api Key is missing");
            return;
        }

        var apiKey = extractedApiKey.ToString();
        if (!IsValidApiKey(apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid Api Key");
            return;
        }

        await _next(context);
    }

    private bool IsValidApiKey(string apiKey)
    {
        var hashedApiKeyFromEnv = _configuration["ApiKey:Hash"];
        if (string.IsNullOrEmpty(hashedApiKeyFromEnv))
        {
            throw new InvalidOperationException("API_KEY_HASH environment variable is not set");
        }

        var hashedInputKey = HashApiKey(apiKey);

        return hashedApiKeyFromEnv.Equals(hashedInputKey, StringComparison.OrdinalIgnoreCase);
    }

    private static string HashApiKey(string apiKey)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(bytes);
        }
    }
}
