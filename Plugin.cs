using System;
using JellyCleaner.Configuration;
using JellyCleaner.Services;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace JellyCleaner
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "Jelly Cleaner";
        public override Guid Id => Guid.Parse("PUT-YOUR-GUID-HERE");

        public override void ConfigureServices(IServiceCollection services)
        {
            // Persist plugin settings
            services.AddSingleton<IPluginConfigurationManager<PluginConfiguration>, PluginConfigurationManager<PluginConfiguration>>();
            // Register cleanup background task
            services.AddHostedService<CleanupService>();
        }
    }
}