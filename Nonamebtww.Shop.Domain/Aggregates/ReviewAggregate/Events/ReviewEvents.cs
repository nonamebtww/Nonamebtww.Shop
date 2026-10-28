using Nonamebtww.Shop.Domain.Events.Abstractions;

namespace Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate.Events;

public sealed record ReviewCreated(Guid ReviewId, Guid UserId, Guid ProductId, int Rating) : DomainEvent;

public sealed record ReviewEdited(Guid ReviewId, Guid ProductId, int OldRating, int NewRating) : DomainEvent;
