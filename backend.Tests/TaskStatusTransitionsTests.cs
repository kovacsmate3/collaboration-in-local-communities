using Backend.Domain.Tasks;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class TaskStatusTransitionsTests
{
    [Theory]
    [InlineData(DomainTaskStatus.Open, DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.Open, DomainTaskStatus.Cancelled)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.PendingApproval)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.Cancelled)]
    [InlineData(DomainTaskStatus.PendingApproval, DomainTaskStatus.Completed)]
    [InlineData(DomainTaskStatus.PendingApproval, DomainTaskStatus.Cancelled)]
    public void CanTransition_AllowsValidEdges(DomainTaskStatus from, DomainTaskStatus to)
    {
        Assert.True(TaskStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    // Skipping intermediate states is not allowed.
    [InlineData(DomainTaskStatus.Open, DomainTaskStatus.PendingApproval)]
    [InlineData(DomainTaskStatus.Open, DomainTaskStatus.Completed)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.Completed)]
    [InlineData(DomainTaskStatus.PendingApproval, DomainTaskStatus.InProgress)]
    // No-op self transitions are invalid.
    [InlineData(DomainTaskStatus.Open, DomainTaskStatus.Open)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.Completed, DomainTaskStatus.Completed)]
    // Terminal states have no outgoing transitions.
    [InlineData(DomainTaskStatus.Completed, DomainTaskStatus.Open)]
    [InlineData(DomainTaskStatus.Completed, DomainTaskStatus.Cancelled)]
    [InlineData(DomainTaskStatus.Cancelled, DomainTaskStatus.Open)]
    [InlineData(DomainTaskStatus.Cancelled, DomainTaskStatus.Completed)]
    public void CanTransition_RejectsInvalidEdges(DomainTaskStatus from, DomainTaskStatus to)
    {
        Assert.False(TaskStatusTransitions.CanTransition(from, to));
    }
}
