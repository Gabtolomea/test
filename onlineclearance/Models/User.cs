public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? MiddleInitial { get; set; }
    public string? SuffixName { get; set; }

    public string? ESignaturePath { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? Role { get; set; }
}