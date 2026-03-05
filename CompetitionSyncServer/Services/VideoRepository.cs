using CompetitionSyncServer.Models;
using System.Text.Json;

namespace CompetitionSyncServer.Services;

public sealed class VideoRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _storagePath;

    public VideoRepository(IConfiguration configuration)
    {
        var fromConfig = configuration["Storage:VideoFilePath"];
        _storagePath = string.IsNullOrWhiteSpace(fromConfig)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "videos.json")
            : fromConfig;
    }

    public async Task<IReadOnlyList<MatchVideo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return await ReadInternalAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddOrUpdateAsync(MatchVideo video, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var videos = await ReadInternalAsync(cancellationToken);
            var existingIndex = videos.FindIndex(v => v.Id == video.Id);
            if (existingIndex >= 0)
            {
                videos[existingIndex] = video;
            }
            else
            {
                videos.Insert(0, video);
            }

            videos = videos
                .OrderByDescending(v => v.PublishedAtUtc)
                .ToList();

            await SaveInternalAsync(videos, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var videos = await ReadInternalAsync(cancellationToken);
            videos.RemoveAll(v => v.Id == videoId);
            await SaveInternalAsync(videos, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<MatchVideo>> ReadInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return new List<MatchVideo>();
        }

        await using var stream = File.OpenRead(_storagePath);
        var videos = await JsonSerializer.DeserializeAsync<List<MatchVideo>>(stream, JsonOptions, cancellationToken);
        return videos ?? new List<MatchVideo>();
    }

    private async Task SaveInternalAsync(List<MatchVideo> videos, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, videos, JsonOptions, cancellationToken);
    }
}
