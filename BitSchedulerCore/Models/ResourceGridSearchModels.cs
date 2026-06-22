namespace BitSchedulerCore.Models;

public sealed class ResourceGridSearchRequest
{
    public int? StartGridId { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }

    public DateTime EventStartUtc { get; set; }
    public int MaxRingDistance { get; set; } = 3;
    public int MinimumCandidateCount { get; set; } = 10;

    public IReadOnlyCollection<int>? RequiredResourceTypeIds { get; set; }
}

public sealed class ResourceGridSearchResult
{
    public int ResourceId { get; set; }
    public int ResourceGridId { get; set; }
    public int SearchRingDistance { get; set; }

    public double ResourceLatitude { get; set; }
    public double ResourceLongitude { get; set; }

    public double StraightLineDistanceMeters { get; set; }
}
