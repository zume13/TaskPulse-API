namespace Application.Dto;

public record ResetPasswordRequestDto(string Token, string NewPassword);