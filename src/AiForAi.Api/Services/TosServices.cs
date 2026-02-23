using AiForAi.Api.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AiForAi.Api.Services;

public sealed class FileTosProvider : ITosProvider
{
    private readonly AppOptions _options;
    private readonly IWebHostEnvironment _environment;

    public FileTosProvider(IOptions<AppOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public string GetCurrentTosVersion()
    {
        if (!string.IsNullOrWhiteSpace(_options.CurrentTosVersion))
        {
            return _options.CurrentTosVersion.Trim();
        }

        var tosText = ReadTosText();
        if (TryExtractExplicitVersion(tosText, out var explicitVersion))
        {
            return explicitVersion;
        }

        return ComputeStableVersionHash(tosText);
    }

    public async Task<string> GetCurrentTosTextAsync(CancellationToken ct)
    {
        var path = Path.Combine(_environment.ContentRootPath, _options.TosFilePath);
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(path, ct);
    }

    private string ReadTosText()
    {
        var path = Path.Combine(_environment.ContentRootPath, _options.TosFilePath);
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        return File.ReadAllText(path);
    }

    private static bool TryExtractExplicitVersion(string tosText, out string version)
    {
        version = string.Empty;

        if (string.IsNullOrWhiteSpace(tosText))
        {
            return false;
        }

        var lines = tosText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(l => l.Trim());

        var versionLine = lines.FirstOrDefault(l => l.StartsWith("Version:", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(versionLine))
        {
            return false;
        }

        version = versionLine["Version:".Length..].Trim();
        return !string.IsNullOrWhiteSpace(version);
    }

    private static string ComputeStableVersionHash(string text)
    {
        var normalized = text.Replace("\r\n", "\n");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"sha256:{hex}";
    }
}

public sealed class TosPolicy : ITosPolicy
{
    public bool IsAccepted(Models.Agent agent, string currentVersion)
    {
        return string.Equals(agent.AcceptedTosVersion, currentVersion, StringComparison.Ordinal);
    }
}
