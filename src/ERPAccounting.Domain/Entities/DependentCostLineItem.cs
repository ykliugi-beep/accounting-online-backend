using System;
using System.ComponentModel.DataAnnotations;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Stavka zavisnog troška (tblDokumentTroskoviStavka)
/// KRITIČNO: Sadrži RowVersion za ETag konkurentnost
/// </summary>
public class DependentCostLineItem : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentCostId { get; set; }
    
    public int VrstaVozacaId { get; set; }
    public int NacinDeljenjaTroskovaId { get; set; }
    public decimal Iznos { get; set; }
    public bool ObracunPoreza { get; set; } = true;
    public int PoreskaStopa { get; set; } = 20;
    
    /// <summary>
    /// JSON array artikal IDs: [1, 2, 3]
    /// </summary>
    public string ArtikalIds { get; set; } = "[]";
    
    // KRITIČNO: ETag konkurentnost
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation
    public virtual DocumentCost DocumentCost { get; set; } = null!;
}
