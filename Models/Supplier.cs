using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int MinQuantity { get; set; }

    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    [NotMapped]
    public string SupplierName => Supplier?.Name ?? string.Empty;

    [NotMapped]
    public bool IsLowStock => Quantity < MinQuantity;

    [NotMapped]
    public string StatusText => IsLowStock ? "⚠️ Под минимума" : "✅ В норма";
}