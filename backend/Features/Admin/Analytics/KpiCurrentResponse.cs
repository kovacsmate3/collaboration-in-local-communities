using Backend.Infrastructure.Persistence.Analytics;

namespace Backend.Features.Admin.Analytics;

public sealed record KpiCurrentResponse(
    long RegisteredUsers,
    long ActiveUsers7d,
    long TasksPosted7d,
    long CompletedTasks7d,
    decimal CompletionRate7d)
{
    public static KpiCurrentResponse FromReadModel(KpiCurrent? current)
    {
        return current is null
            ? new KpiCurrentResponse(0, 0, 0, 0, 0)
            : new KpiCurrentResponse(
                current.RegisteredUsers,
                current.ActiveUsers7d,
                current.TasksPosted7d,
                current.CompletedTasks7d,
                current.CompletionRate7d);
    }
}
