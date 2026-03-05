using CompetitionSyncServer.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace CompetitionSyncServer.Services;

public sealed class CompetitionCatalogService
{
    private static readonly Regex CompetitionRegex = new(
        "<a[^>]*href=[\"'](?<url>/game/\\d+)[\"'][^>]*>(?<text>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly HttpClient _httpClient;

    public CompetitionCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<CompetitionInfo>> GetCompetitionsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("https://competition.robot-soccer-kit.com/", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new List<CompetitionInfo>();
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<CompetitionInfo>();

        foreach (Match match in CompetitionRegex.Matches(html))
        {
            var relativeUrl = match.Groups["url"].Value.Trim();
            var decoded = WebUtility.HtmlDecode(match.Groups["text"].Value);
            var cleanName = Regex.Replace(decoded, "<.*?>", string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(relativeUrl) || string.IsNullOrWhiteSpace(cleanName))
            {
                continue;
            }

            if (!seen.Add(relativeUrl))
            {
                continue;
            }

            results.Add(new CompetitionInfo
            {
                Name = cleanName,
                GameUrl = $"https://competition.robot-soccer-kit.com{relativeUrl}"
            });
        }

        return results;
    }
}
