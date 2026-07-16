namespace PelicanTown.SharedKernel.Domain;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
