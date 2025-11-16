using System;
using System.ComponentModel.DataAnnotations;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Stavka dokumenta (tblStavkaDokumenta)
/// KRITIČNO: Sadrži RowVersion za ETag konkurentnost
/// </summary>
public class DocumentLineItem : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    
    // Osnovni podaci
    public int ArtikalId { get; set; }
    public decimal Kolicina { get; set; }
    public decimal FakturnaCena { get; set; }
    public decimal Rabat { get; set; } = 0;
    public decimal Marza { get; set; } = 0;
    public int PoreskaStopa { get; set; } = 20;
    
    // Obračun
    public bool ObracunAkciza { get; set; } = false;
    public bool ObracunPorez { get; set; } = true;
    
    // KRITIČNO: ETag konkurentnost
    /// <summary>
    /// RowVersion za optimistic concurrency control
    /// Automatski se ažurira od strane SQL Servera pri svakom UPDATE-u
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation
    public virtual Document Document { get; set; } = null!;
}
