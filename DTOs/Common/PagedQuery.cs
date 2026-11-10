using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs.Common;

/// <summary>Query-string paging parameters: <c>?page=1&amp;pageSize=20</c>.</summary>
public class PagedQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    // The upper bound keeps (Page - 1) * PageSize inside the int range used by Skip().
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = DefaultPageSize;
}
