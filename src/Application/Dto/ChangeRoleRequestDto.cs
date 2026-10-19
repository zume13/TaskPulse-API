using Domain.Aggregates.User.Enums;

namespace Application.Dto;

public record ChangeRoleRequestDto(Role NewRole);