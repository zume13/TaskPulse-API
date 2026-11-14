namespace Application.Dto;

public record NotificationPageDto(
    IReadOnlyList<NotificationDto> Items,
    int Page,
    int PageSize,
    bool HasMore,
    int UnreadCount);
