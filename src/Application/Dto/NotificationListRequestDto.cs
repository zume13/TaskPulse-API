namespace Application.Dto;

public record NotificationListRequestDto(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20);