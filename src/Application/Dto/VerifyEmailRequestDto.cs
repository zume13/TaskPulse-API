namespace Application.Dto;

public record VerifyEmailRequestDto(Guid UserId, string Token);