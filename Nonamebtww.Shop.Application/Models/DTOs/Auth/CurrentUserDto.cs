using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Application.Models.DTOs.Auth;

public sealed record CurrentUserDto(
  Guid Id,
  string Login,
  string Email,
  string? Phone,
  string FirstName,
  string? LastName,
  UserRole Role) {
  public string FullName => LastName is null ? FirstName : $"{FirstName} {LastName}";
}
