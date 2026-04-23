public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int? AuthorId { get; set; }
    public string Type { get; set; } = "General";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}