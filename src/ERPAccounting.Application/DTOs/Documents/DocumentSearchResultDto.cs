using System.ComponentModel.DataAnnotations;

namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO representing paginated search results for documents with computed pagination properties.
/// Used in document search endpoints to provide consistent pagination metadata.
/// </summary>
public class DocumentSearchResultDto
{
    /// <summary>
    /// Collection of document DTOs for the current page.
    /// </summary>
    [Required]
    public List<DocumentDto> Documents { get; set; } = new();

    /// <summary>
    /// Total number of documents matching the search criteria (all pages).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based indexing).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [Range(1, 1000)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Computed: Total number of pages available for the search results.
    /// Calculated as: (TotalCount + PageSize - 1) / PageSize
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>
    /// Computed: Indicates if there is a previous page available.
    /// Calculated as: PageNumber > 1
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Computed: Indicates if there is a next page available.
    /// Calculated as: PageNumber < TotalPages
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
