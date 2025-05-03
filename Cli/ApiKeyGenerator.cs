using System.Security.Cryptography;
using System.Text;

namespace Pixlmint.Util;

public class ApiKeyGenerator
{
    public static bool GenerateAndPrintKey(string[] args)
    {
        if (args.Length == 0 || args[0] != "generate-key")
            return false;
        Console.WriteLine("API Key Generator Tool");
        Console.WriteLine("=====================");

        // Generate a random API key if not provided
        string apiKey;
        if (args.Length > 1)
        {
            apiKey = args[1];
            Console.WriteLine($"Using provided API key: {apiKey}");
        }
        else
        {
            apiKey = GenerateApiKey();
            Console.WriteLine($"Generated new API key: {apiKey}");
        }

        // Hash the API key
        string hashedKey = HashApiKey(apiKey);

        Console.WriteLine("\nEnvironment variable to set:");
        Console.WriteLine($"API_KEY_HASH={hashedKey}");

        Console.WriteLine("\nIn Windows CMD:");
        Console.WriteLine($"set API_KEY_HASH={hashedKey}");

        Console.WriteLine("\nIn Windows PowerShell:");
        Console.WriteLine($"$env:API_KEY_HASH=\"{hashedKey}\"");

        Console.WriteLine("\nIn Linux/macOS bash/zsh:");
        Console.WriteLine($"export API_KEY_HASH=\"{hashedKey}\"");

        Console.WriteLine(
            "\nFor .NET development with dotnet user-secrets (recommended for development):"
        );
        Console.WriteLine($"dotnet user-secrets set \"API_KEY_HASH\" \"{hashedKey}\"");

        Console.WriteLine("\nIn Dockerfile or docker-compose.yml:");
        Console.WriteLine($"API_KEY_HASH: {hashedKey}");

        Console.WriteLine("\nTo use this API key in requests, add the following header:");
        Console.WriteLine($"X-API-Key: {apiKey}");

        return true;
    }

    static string GenerateApiKey()
    {
        // Generate a random 32-byte API key
        byte[] keyBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        // Convert to a Base64 string and make it URL-safe
        string apiKey = Convert
            .ToBase64String(keyBytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");

        return apiKey;
    }

    static string HashApiKey(string apiKey)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(bytes);
        }
    }
}
