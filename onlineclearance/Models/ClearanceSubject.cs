public class ClearanceSubject
{
    public int Id { get; set; }
    public string? StudentNumber { get; set; }
    public string MisCode { get; set; } = "";
    public int? Status { get; set; }
    public string? Remarks { get; set; }
    public int? PeriodId { get; set; }
    public DateTime? SignedAt { get; set; }
}