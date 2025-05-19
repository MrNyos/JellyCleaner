public class DeleteOldWatchedOptions
{
    // How many days since last watched before we delete
    public int DaysThreshold { get; set; } = 60;

    // Apply to movies?
    public bool ProcessMovies { get; set; } = true;

    // Apply to TV episodes?
    public bool ProcessShows { get; set; } = true;

    // Exact item names to never delete
    public List<string> ExcludedTitles { get; set; } = new List<string>();
}
