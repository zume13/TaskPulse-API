namespace Application.Dto;

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);