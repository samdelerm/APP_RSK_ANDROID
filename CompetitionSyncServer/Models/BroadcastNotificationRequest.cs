namespace CompetitionSyncServer.Models;

public sealed class BroadcastNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
