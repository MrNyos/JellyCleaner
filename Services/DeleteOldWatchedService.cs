public class DeleteOldWatchedService : IHostedService, IDisposable
{
    private readonly ILibraryManager _library;
    private readonly IUserDataManager _userData;
    private readonly IFileSystemManager _fs;
    private readonly IPluginConfigurationManager<DeleteOldWatchedOptions> _configMgr;
    private Timer _timer;

    public DeleteOldWatchedService(
        ILibraryManager library,
        IUserDataManager userData,
        IFileSystemManager fs,
        IPluginConfigurationManager<DeleteOldWatchedOptions> configMgr)
    {
        _library = library;
        _userData = userData;
        _fs = fs;
        _configMgr = configMgr;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // run once at startup, then every 24h
        _timer = new Timer(DoScan, null, TimeSpan.Zero, TimeSpan.FromHours(24));
        return Task.CompletedTask;
    }

    private async void DoScan(object _)
    {
        var cfg = _configMgr.Load();
        var cutoff = DateTime.UtcNow.AddDays(-cfg.DaysThreshold);

        // scan all users (or pick one admin user)
        var user = _userData.Users.First();

        // fetch all library items
        var allItems = _library.RootFolders
            .SelectMany(r => _library.GetItemList(new InternalItemsQuery { Recursive = true }))
            .Where(i => i.LocationType == LocationType.FileSystem);

        foreach (var item in allItems)
        {
            // skip by type
            if (item is Movie && !cfg.ProcessMovies) continue;
            if (item is Episode && !cfg.ProcessShows) continue;
            // skip exclusions
            if (cfg.ExcludedTitles.Contains(item.Name, StringComparer.OrdinalIgnoreCase)) continue;

            // get user-specific watched data
            var data = _userData.GetUserData(user, item);
            if (data.PlayedPercentage >= 100 && data.LastPlayedDate < cutoff)
            {
                // delete both database record + file(s)
                foreach (var path in item.MediaStreams.SelectMany(m => m.GetMediaFilePaths()))
                    _fs.Delete(path);
                await _library.RemoveItem(item);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
    public void Dispose() => _timer?.Dispose();
}
