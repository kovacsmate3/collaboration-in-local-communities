using JetBrains.Annotations;

namespace Backend.Features.Admin.Analytics;

/// <param name="Pct">
/// Relative weight for bar width.
/// task-status / compensation-mix: share of total (0–100).
/// category-demand: share of the top category (top = 100).
/// </param>
[PublicAPI]
public sealed record ChartEntry(string Label, int Count, double Pct);

[PublicAPI]
public sealed record ChartDataResponse(IReadOnlyList<ChartEntry> Entries);
