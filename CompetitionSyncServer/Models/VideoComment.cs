namespace CompetitionSyncServer.Models;

public sealed class VideoComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Author { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
