using Domain.Aggregates.User.Enum;

namespace Application.Dto;

public record ChangeRoleRequestDto(Role NewRole);