namespace Application.Dto;

public record NotificationListDto(IReadOnlyList<NotificationDto> Items, int Page, int PageSize, bool HasMore);