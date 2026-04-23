public class ClearanceOrganization
{
    public int Id { get; set; }
    public string? StudentNumber { get; set; }
    public int? OrgId { get; set; }
    public string? OrgName { get; set; }
    public string? OrgSignatory { get; set; }
    public int? PeriodId { get; set; }
    public int? Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}