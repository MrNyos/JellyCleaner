using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using JellyCleaner.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Library;
using MediaBrowser.Data.Enums;

namespace JellyCleaner.Services
{
    public class CleanupService : BackgroundService
    {
        private readonly ILibraryManager _library;
        private readonly IUserDataManager _userData;
        private readonly IFileSystem _fs;
        private readonly IPluginConfigurationManager<PluginConfiguration> _config;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(
            ILibraryManager library,
            IUserDataManager userData,
            IFileSystem fileSystem,
            IPluginConfigurationManager<PluginConfiguration> config,
            ILogger<CleanupService> logger)
        {
            _library = library;
            _userData = userData;
            _fs = fileSystem;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DoCleanup();
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                await DoCleanup();
            }
        }

        private Task DoCleanup()
        {
            var cfg = _config.Load();
            var cutoff = DateTime.UtcNow.AddDays(-cfg.DaysThreshold);
            var user = _userData.Users.First();
            var items = _library.RootFolders
                .SelectMany(r => _library.GetItemList(new InternalItemsQuery { Recursive = true }))
                .Where(i => i.LocationType == LocationType.FileSystem);

            foreach (var item in items)
            {
                if (item is Movie && !cfg.ProcessMovies) continue;
                if (item is Episode && !cfg.ProcessShows) continue;
                if (cfg.ExcludedTitles.Contains(item.Name, StringComparer.OrdinalIgnoreCase)) continue;

                var data = _userData.GetUserData(user, item);
                if (data.PlayedPercentage >= 100 && data.LastPlayedDate < cutoff)
                {
                    foreach (var path in item.GetMediaPaths())
                        _fs.Delete(path);
                    _library.RemoveItem(item).Wait();
                    _logger.LogInformation("Deleted {Item} watched on {Date}", item.Name, data.LastPlayedDate);
                }
            }

            return Task.CompletedTask;
        }
    }
}