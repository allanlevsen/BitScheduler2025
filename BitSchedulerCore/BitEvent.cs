using BitSchedulerCore.Models;

namespace BitSchedulerCore;

public class BitEvent : AuditableEntity
{
    public int BitEventId { get; set; }

    public int BitClientId { get; set; }
    public BitClient BitClient { get; set; } = null!;

    public int BitResourceId { get; set; }
    public BitResource BitResource { get; set; } = null!;

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }

    public string? StartAddress { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public int? StartHexGridId { get; set; }

    public string? EndAddress { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }
    public int? EndHexGridId { get; set; }

    public bool RequiresTransportation { get; set; }
    public bool RequiresReturnTransportation { get; set; }
}
