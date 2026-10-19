using Domain.Aggregates.User.Enums;

namespace Application.Dto;

public record UserDto(Guid Id, string Email, string FirstName, string LastName, Role Role, bool IsEmailVerified);