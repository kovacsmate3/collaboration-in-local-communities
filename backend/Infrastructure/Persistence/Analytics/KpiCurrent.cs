namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class KpiCurrent
{
    public long RegisteredUsers { get; set; }
    public long ActiveUsers7d { get; set; }
    public long TasksPosted7d { get; set; }
    public long CompletedTasks7d { get; set; }
    public decimal CompletionRate7d { get; set; }
}
