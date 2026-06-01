namespace BitSchedulerCore
{
    public class BitResourceType
    {
        public int BitResourceTypeId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<BitResource> BitResources { get; set; } = new List<BitResource>();
    }
}
