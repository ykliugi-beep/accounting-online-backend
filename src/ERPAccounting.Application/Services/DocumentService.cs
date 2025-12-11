using AutoMapper;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = ERPAccounting.Common.Exceptions.ValidationException;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Application service responsible for orchestrating document CRUD operations with validation and concurrency handling.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDocumentDto> _createValidator;
    private readonly IValidator<UpdateDocumentDto> _updateValidator;
    private readonly IValidator<DocumentQueryParameters> _queryValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateDocumentDto> createValidator,
        IValidator<UpdateDocumentDto> updateValidator,
        IValidator<DocumentQueryParameters> queryValidator,
        IMapper mapper,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<DocumentDto>> GetDocumentsAsync(DocumentQueryParameters query)
    {
        query ??= new DocumentQueryParameters();
        await ValidateAsync(_queryValidator, query);

        var (items, totalCount) = await _documentRepository.GetPaginatedAsync(query.Page, query.PageSize, query.Search);
        var dtoItems = _mapper.Map<List<DocumentDto>>(items);

        return new PaginatedResult<DocumentDto>(dtoItems, totalCount, query.Page, query.PageSize);
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(int documentId)
    {
        var entity = await _documentRepository.GetByIdAsync(documentId);
        return entity is null ? null : _mapper.Map<DocumentDto>(entity);
    }

    public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await ValidateAsync(_createValidator, dto);

        var entity = _mapper.Map<Document>(dto);

        await _documentRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentDto>(entity);
    }

    public async Task<DocumentDto> UpdateDocumentAsync(int documentId, byte[] expectedRowVersion, UpdateDocumentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await ValidateAsync(_updateValidator, dto);

        var entity = await _documentRepository.GetByIdAsync(documentId, track: true);

        if (entity is null)
        {
            throw new NotFoundException(ErrorMessages.DocumentNotFound, documentId.ToString(), nameof(Document));
        }

        if (!MatchesRowVersion(entity, expectedRowVersion))
        {
            _logger.LogWarning("RowVersion mismatch detected for document {DocumentId}", documentId);
            var currentEtag = entity.DokumentTimeStamp is null ? string.Empty : Convert.ToBase64String(entity.DokumentTimeStamp);
            var expectedEtag = Convert.ToBase64String(expectedRowVersion);
            throw new ConflictException(
                ErrorMessages.ConcurrencyConflict,
                documentId.ToString(),
                nameof(Document),
                expectedEtag,
                currentEtag);
        }

        _mapper.Map(dto, entity);

        _documentRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentDto>(entity);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        var entity = await _documentRepository.GetByIdAsync(documentId, track: true);

        if (entity is null)
        {
            return false;
        }

        // Hard delete - remove from database
        _documentRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Pretraga dokumenata sa naprednijim filterima
    /// </summary>
    public async Task<DocumentSearchResultDto> SearchDocumentsAsync(DocumentSearchDto searchDto)
    {
        ArgumentNullException.ThrowIfNull(searchDto);

        // Počni sa svim dokumentima
        var query = await _documentRepository.GetAllAsync();

        // Primeni filtere
        if (!string.IsNullOrWhiteSpace(searchDto.DocumentNumber))
        {
            query = query.Where(d => d.BrojDokumenta != null && d.BrojDokumenta.Contains(searchDto.DocumentNumber));
        }

        if (searchDto.PartnerId.HasValue)
        {
            query = query.Where(d => d.IDPartner == searchDto.PartnerId.Value);
        }

        if (searchDto.DateFrom.HasValue)
        {
            query = query.Where(d => d.Datum >= searchDto.DateFrom.Value);
        }

        if (searchDto.DateTo.HasValue)
        {
            query = query.Where(d => d.Datum <= searchDto.DateTo.Value);
        }

        if (searchDto.StatusId.HasValue)
        {
            query = query.Where(d => d.IDStatus == searchDto.StatusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.DocumentTypeCode))
        {
            query = query.Where(d => d.IDVrstaDokumenta == searchDto.DocumentTypeCode);
        }

        // Ukupan broj pre paginacije
        var totalCount = query.Count();

        // Sortiranje
        query = ApplySorting(query, searchDto.SortBy, searchDto.SortDirection);

        // Paginacija
        var items = query
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        var documentDtos = _mapper.Map<List<DocumentDto>>(items);

        return new DocumentSearchResultDto
        {
            Documents = documentDtos,
            TotalCount = totalCount,
            PageNumber = searchDto.PageNumber,
            PageSize = searchDto.PageSize
        };
    }

    /// <summary>
    /// Primenjuje sortiranje na query
    /// </summary>
    private static IEnumerable<Document> ApplySorting(IEnumerable<Document> query, string? sortBy, string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return (sortBy?.ToLower()) switch
        {
            "documentnumber" => isDescending 
                ? query.OrderByDescending(d => d.BrojDokumenta) 
                : query.OrderBy(d => d.BrojDokumenta),
            "documentdate" or "date" => isDescending 
                ? query.OrderByDescending(d => d.Datum) 
                : query.OrderBy(d => d.Datum),
            "partner" => isDescending 
                ? query.OrderByDescending(d => d.IDPartner) 
                : query.OrderBy(d => d.IDPartner),
            "status" => isDescending 
                ? query.OrderByDescending(d => d.IDStatus) 
                : query.OrderBy(d => d.IDStatus),
            _ => isDescending 
                ? query.OrderByDescending(d => d.Datum) 
                : query.OrderBy(d => d.Datum) // Default: sort by date
        };
    }

    private static bool MatchesRowVersion(Document entity, byte[] expectedRowVersion)
    {
        if (entity.DokumentTimeStamp is null)
        {
            return expectedRowVersion is null || expectedRowVersion.Length == 0;
        }

        return expectedRowVersion != null && entity.DokumentTimeStamp.SequenceEqual(expectedRowVersion);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
    {
        var validationResult = await validator.ValidateAsync(instance);
        if (validationResult.IsValid)
        {
            return;
        }

        var errors = validationResult.Errors
            .GroupBy(failure => failure.PropertyName ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(static failure => failure.ErrorMessage).ToArray());

        throw new ValidationException(ErrorMessages.ValidationFailed, errors);
    }
}
