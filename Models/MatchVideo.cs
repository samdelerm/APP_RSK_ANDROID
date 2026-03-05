namespace CompetitionSyncServer.Models;

public sealed class MatchVideo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CompetitionName { get; set; } = string.Empty;
    public string CompetitionUrl { get; set; } = string.Empty;
    public string MatchName { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
}
