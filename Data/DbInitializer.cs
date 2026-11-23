using WarehouseApp.Models;

namespace WarehouseApp.Data;

public static class DbInitializer
{
    public static void Initialize()
    {
        using var db = new WarehouseContext();
        db.Database.EnsureCreated();

        if (db.Suppliers.Any())
            return;

        var suppliers = new[]
        {
            new Supplier { Name = "Техно Трейд ЕООД", Phone = "+359 52 600 111", Email = "office@technotrade.bg" },
            new Supplier { Name = "Мегастор АД",      Phone = "+359 2 900 2200", Email = "sales@megastore.bg" },
            new Supplier { Name = "Офис Плюс ООД",    Phone = "+359 32 555 777", Email = "info@officeplus.bg" }
        };
        db.Suppliers.AddRange(suppliers);
        db.SaveChanges();

        db.Products.AddRange(
            new Product { Name = "Лаптоп Lenovo ThinkPad", Price = 1450.00m, Quantity = 3,  MinQuantity = 5,  SupplierId = suppliers[0].Id },
            new Product { Name = "Копирна хартия А4 (кашон)", Price = 18.50m, Quantity = 4,  MinQuantity = 20, SupplierId = suppliers[2].Id },
            new Product { Name = "Безжична мишка Logitech", Price = 25.90m,  Quantity = 40, MinQuantity = 10, SupplierId = suppliers[0].Id },
            new Product { Name = "Тонер HP 85A",            Price = 89.00m,  Quantity = 12, MinQuantity = 5,  SupplierId = suppliers[1].Id },
            new Product { Name = "Монитор 24\" Dell",       Price = 289.00m, Quantity = 8,  MinQuantity = 6,  SupplierId = suppliers[1].Id }
        );
        db.SaveChanges();
    }
}
