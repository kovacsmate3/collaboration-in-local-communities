using JetBrains.Annotations;

namespace Backend.Features.Admin.Analytics;

[PublicAPI]
public sealed record ChartEntry(string Label, int Count, double Pct);

[PublicAPI]
public sealed record ChartDataResponse(IReadOnlyList<ChartEntry> Entries);
