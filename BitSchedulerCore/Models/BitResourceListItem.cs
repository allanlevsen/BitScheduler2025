namespace BitSchedulerCore.Models;

public class BitResourceListItem
{
    public int BitResourceId { get; set; }
    public int BitResourceTypeId { get; set; }
    public string ResourceTypeName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
