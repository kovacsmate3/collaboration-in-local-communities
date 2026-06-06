using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Domain.Tasks;

/// <summary>
/// Single source of truth for the task status lifecycle (finite state machine).
///
/// Lifecycle:
///     Open ──► InProgress ──► PendingApproval ──► Completed
///       │           │                 │
///       └───────────┴─────────────────┴──► Cancelled
///
/// Completed and Cancelled are terminal: they have no outgoing transitions.
/// Controllers consult this map so an invalid transition fails the same way
/// everywhere instead of each call site re-deriving the rules.
/// </summary>
public static class TaskStatusTransitions
{
    private static readonly Dictionary<DomainTaskStatus, IReadOnlySet<DomainTaskStatus>> _allowed =
        new()
        {
            [DomainTaskStatus.Open] = new HashSet<DomainTaskStatus>
            {
                DomainTaskStatus.InProgress,
                DomainTaskStatus.Cancelled
            },
            [DomainTaskStatus.InProgress] = new HashSet<DomainTaskStatus>
            {
                DomainTaskStatus.PendingApproval,
                DomainTaskStatus.Cancelled
            },
            [DomainTaskStatus.PendingApproval] = new HashSet<DomainTaskStatus>
            {
                DomainTaskStatus.Completed,
                DomainTaskStatus.Cancelled
            },
            [DomainTaskStatus.Completed] = new HashSet<DomainTaskStatus>(),
            [DomainTaskStatus.Cancelled] = new HashSet<DomainTaskStatus>()
        };

    public static bool CanTransition(DomainTaskStatus from, DomainTaskStatus to) =>
        _allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
