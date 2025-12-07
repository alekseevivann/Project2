namespace Project2;

public class Recording
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedDate { get; set; }
    public string DurationDisplay => Duration.ToString(@"mm\:ss");
    public string DateDisplay => CreatedDate.ToString("dd.MM.yyyy HH:mm");

    public Recording()
    {
        Id = Guid.NewGuid().ToString();
        CreatedDate = DateTime.Now;
    }
}