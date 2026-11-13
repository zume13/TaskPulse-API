using Domain.Aggregates.Notifications.Enums;

namespace Application.Dto;

public record NotificationDto(
Guid Id, NotificationType Type, string Title, string Message, Guid? WorkspaceId, NotificationTarget TargetType, Guid? TargetId, 
bool IsRead, DateTime CreatedAtUtc, DateTime? ReadAtUtc);