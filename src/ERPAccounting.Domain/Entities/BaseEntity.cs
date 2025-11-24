using System;
using System.ComponentModel.DataAnnotations.Schema;
using ERPAccounting.Domain.Interfaces;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Bazna klasa za sve entitete sa audit funkcionalnostima.
/// Većina audit polja su [NotMapped] i popunjavaju se kroz AuditInterceptor pri SaveChanges(),
/// dok je IsDeleted mapiran radi implementacije soft delete obrasca.
/// </summary>
public abstract class BaseEntity : IEntity
{
    /// <summary>
    /// Timestamp kada je entitet kreiran. Automatski se setuje pri INSERT.
    /// </summary>
    [NotMapped]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp poslednje izmene. Automatski se setuje pri UPDATE.
    /// </summary>
    [NotMapped]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Korisničko ime koje je kreiralo entitet. Default: "API_DEFAULT_USER"
    /// </summary>
    [NotMapped]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Korisničko ime koje je poslednje izmenilo entitet.
    /// </summary>
    [NotMapped]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Oznaka za soft delete. Entitet nije fizički obrisan iz baze.
    /// Query filter automatski filtrira IsDeleted = true zapise.
    /// </summary>
    [Column("IsDeleted")]
    public bool IsDeleted { get; set; } = false;
}