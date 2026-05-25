namespace Backend.Domain.Enums;

public enum ActivityEventType
{
    UserRegistered,
    UserLoggedIn,
    ProfileUpdated,
    TaskPosted,
    TaskApplicationSubmitted,
    TaskApplicationRejected,
    TaskApplicationWithdrawn,
    TaskAccepted,
    TaskCancelled,
    TaskCompletionSubmitted,
    TaskCompleted,
    ReviewSubmitted,
    MessageSent
}
