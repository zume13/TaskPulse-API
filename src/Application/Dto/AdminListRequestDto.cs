namespace Application.Dto;

/// <summary>Query string of the admin directory lists, e.g. ?includeArchived=false&amp;page=2&amp;pageSize=50.</summary>
public record AdminListRequestDto(
    bool IncludeArchived = true,
    int Page = 1,
    int PageSize = 20);
