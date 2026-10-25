namespace Application.Dto;

public record ChangeEmailRequestDto(string NewEmail, string CurrentPassword);