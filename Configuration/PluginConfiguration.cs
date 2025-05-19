using System.Collections.Generic;

namespace JellyCleaner.Configuration
{
    public class PluginConfiguration
    {
        public int DaysThreshold { get; set; } = 60;
        public bool ProcessMovies { get; set; } = true;
        public bool ProcessShows { get; set; } = true;
        public List<string> ExcludedTitles { get; set; } = new List<string>();
    }
}