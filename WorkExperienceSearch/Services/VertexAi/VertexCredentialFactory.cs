using System.Text;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace Work_Experience_Search.Services.VertexAi;

internal static class VertexCredentialFactory
{
    public static GoogleCredential Create(VertexAiOptions options, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(options.CredentialsFile))
        {
            logger.LogInformation("Using Vertex AI credentials file at {CredentialsFile}", options.CredentialsFile);
#pragma warning disable CS0618
            return GoogleCredential.FromFile(options.CredentialsFile);
#pragma warning restore CS0618
        }

        if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
        {
            var json = Normalise(options.CredentialsJson);

            try
            {
#pragma warning disable CS0618
                return GoogleCredential.FromJson(json);
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse VertexAi:CredentialsJson (length: {Length}). Ensure the GitHub secret contains the full service account JSON.", json.Length);
                throw new InvalidOperationException("Invalid Vertex AI credentials JSON provided. Double-check the secret value or supply a credentials file path.", ex);
            }
        }

        logger.LogInformation("No explicit Vertex AI credentials supplied; using application default credentials.");
        return GoogleCredential.GetApplicationDefault();
    }

    private static string Normalise(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Contains("\\n"))
        {
            trimmed = trimmed.Replace("\\n", "\n");
        }

        var base64Decoded = TryBase64Decode(trimmed);
        return base64Decoded ?? trimmed;
    }

    private static string? TryBase64Decode(string value)
    {
        if (value.Length % 4 != 0) return null;

        try
        {
            var bytes = Convert.FromBase64String(value);
            var decoded = Encoding.UTF8.GetString(bytes);
            return decoded.TrimStart().StartsWith("{") ? decoded : null;
        }
        catch
        {
            return null;
        }
    }
}
