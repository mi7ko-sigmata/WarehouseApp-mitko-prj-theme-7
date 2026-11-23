using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;
using WarehouseApp.Models;
using WarehouseApp.Services;

namespace WarehouseApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private List<Product> _allProducts = new();

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();

    // ===== Банер и филтър =====
    [ObservableProperty]
    private bool showOnlyLow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLowStock))]
    [NotifyPropertyChangedFor(nameof(BannerText))]
    private int lowStockCount;

    public bool HasLowStock => LowStockCount > 0;

    public string BannerText => HasLowStock
        ? $"⚠️ Внимание! Артикули с критично ниско количество (под минимума): {LowStockCount}"
        : "✅ Всички артикули са с достатъчна наличност.";

    partial void OnShowOnlyLowChanged(bool value) => ApplyFilter();

    // ===== Форма за артикули =====
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProductCommand))]
    private Product? selectedProduct;

    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private string productPrice = "0";
    [ObservableProperty] private string productQuantity = "0";
    [ObservableProperty] private string productMinQuantity = "0";
    [ObservableProperty] private int? productSupplierId;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null) return;
        ProductName = value.Name;
        ProductPrice = value.Price.ToString("0.00", CultureInfo.CurrentCulture);
        ProductQuantity = value.Quantity.ToString(CultureInfo.CurrentCulture);
        ProductMinQuantity = value.MinQuantity.ToString(CultureInfo.CurrentCulture);
        ProductSupplierId = value.SupplierId;
    }

    // ===== Форма за доставчици =====
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateSupplierCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSupplierCommand))]
    private Supplier? selectedSupplier;

    [ObservableProperty] private string supplierName = string.Empty;
    [ObservableProperty] private string supplierPhone = string.Empty;
    [ObservableProperty] private string supplierEmail = string.Empty;

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value is null) return;
        SupplierName = value.Name;
        SupplierPhone = value.Phone;
        SupplierEmail = value.Email;
    }

    public MainViewModel()
    {
        LoadData();
    }

    // ===== Зареждане на данни =====
    private void LoadData()
    {
        using var db = new WarehouseContext();

        var suppliers = db.Suppliers.AsNoTracking().ToList()
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        _allProducts = db.Products.AsNoTracking().Include(p => p.Supplier).ToList()
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        Suppliers.Clear();
        foreach (var s in suppliers) Suppliers.Add(s);

        LowStockCount = _allProducts.Count(p => p.IsLowStock);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Products.Clear();
        foreach (var p in _allProducts.Where(p => !ShowOnlyLow || p.IsLowStock))
            Products.Add(p);
    }

    // ===== Валидиране на числа =====
    private static bool TryParseDecimal(string text, out decimal value)
    {
        text = (text ?? string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseInt(string text, out int value)
    {
        text = (text ?? string.Empty).Trim();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
            || int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private bool ValidateProduct(out decimal price, out int quantity, out int minQuantity)
    {
        var errors = new List<string>();
        price = 0; quantity = 0; minQuantity = 0;

        if (string.IsNullOrWhiteSpace(ProductName))
            errors.Add("• Наименованието на артикула е задължително.");

        if (ProductSupplierId is null)
            errors.Add("• Изберете доставчик от падащото меню.");

        if (!TryParseDecimal(ProductPrice, out price))
            errors.Add("• Цената трябва да е валидно число.");
        else if (price < 0)
            errors.Add("• Цената не може да бъде отрицателна.");

        if (!TryParseInt(ProductQuantity, out quantity))
            errors.Add("• Количеството трябва да е цяло число.");
        else if (quantity < 0)
            errors.Add("• Количеството не може да бъде отрицателно.");

        if (!TryParseInt(ProductMinQuantity, out minQuantity))
            errors.Add("• Минималното количество трябва да е цяло число.");
        else if (minQuantity < 0)
            errors.Add("• Минималното количество не може да бъде отрицателно.");

        if (errors.Count > 0)
        {
            DialogService.Warning("Моля, коригирайте следните грешки:\n\n" + string.Join("\n", errors));
            return false;
        }
        return true;
    }

    private static void WarnIfLow(string name, int quantity, int minQuantity)
    {
        if (quantity < minQuantity)
        {
            DialogService.Warning(
                $"Внимание! Количеството на „{name}“ ({quantity}) е под минималното ({minQuantity}).\n\n" +
                "Препоръчително е да направите нова поръчка към доставчика.");
        }
    }

    // ===== Команди: артикули =====
    private bool HasSelectedProduct() => SelectedProduct is not null;

    [RelayCommand]
    private void AddProduct()
    {
        if (!ValidateProduct(out var price, out var qty, out var min)) return;

        try
        {
            using var db = new WarehouseContext();
            db.Products.Add(new Product
            {
                Name = ProductName.Trim(),
                Price = price,
                Quantity = qty,
                MinQuantity = min,
                SupplierId = ProductSupplierId!.Value
            });
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            DialogService.Error("Грешка при запис на артикула:\n" + ex.Message);
            return;
        }

        var name = ProductName.Trim();
        ClearProduct();
        LoadData();
        WarnIfLow(name, qty, min);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProduct))]
    private void UpdateProduct()
    {
        if (SelectedProduct is null) return;
        if (!ValidateProduct(out var price, out var qty, out var min)) return;

        try
        {
            using var db = new WarehouseContext();
            var entity = db.Products.Find(SelectedProduct.Id);
            if (entity is null)
            {
                DialogService.Error("Артикулът вече не съществува в базата данни.");
                LoadData();
                return;
            }

            entity.Name = ProductName.Trim();
            entity.Price = price;
            entity.Quantity = qty;
            entity.MinQuantity = min;
            entity.SupplierId = ProductSupplierId!.Value;
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            DialogService.Error("Грешка при редакция на артикула:\n" + ex.Message);
            return;
        }

        var name = ProductName.Trim();
        ClearProduct();
        LoadData();
        WarnIfLow(name, qty, min);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProduct))]
    private void DeleteProduct()
    {
        if (SelectedProduct is null) return;
        if (!DialogService.Confirm($"Сигурни ли сте, че искате да изтриете артикул „{SelectedProduct.Name}“?"))
            return;

        try
        {
            using var db = new WarehouseContext();
            var entity = db.Products.Find(SelectedProduct.Id);
            if (entity is not null)
            {
                db.Products.Remove(entity);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            DialogService.Error("Грешка при изтриване:\n" + ex.Message);
            return;
        }

        ClearProduct();
        LoadData();
    }

    [RelayCommand]
    private void ClearProduct()
    {
        SelectedProduct = null;
        ProductName = string.Empty;
        ProductPrice = "0";
        ProductQuantity = "0";
        ProductMinQuantity = "0";
        ProductSupplierId = null;
    }

    // ===== Команди: доставчици =====
    private bool HasSelectedSupplier() => SelectedSupplier is not null;

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateSupplier()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SupplierName))
            errors.Add("• Името на доставчика е задължително.");

        var email = SupplierEmail.Trim();
        if (email.Length > 0 && !IsValidEmail(email))
            errors.Add("• Имейл адресът не е във валиден формат.");

        if (errors.Count > 0)
        {
            DialogService.Warning("Моля, коригирайте следните грешки:\n\n" + string.Join("\n", errors));
            return false;
        }
        return true;
    }

    [RelayCommand]
    private void AddSupplier()
    {
        if (!ValidateSupplier()) return;

        try
        {
            using var db = new WarehouseContext();
            db.Suppliers.Add(new Supplier
            {
                Name = SupplierName.Trim(),
                Phone = SupplierPhone.Trim(),
                Email = SupplierEmail.Trim()
            });
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            DialogService.Error("Грешка при запис на доставчика:\n" + ex.Message);
            return;
        }

        ClearSupplier();
        LoadData();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSupplier))]
    private void UpdateSupplier()
    {
        if (SelectedSupplier is null) return;
        if (!ValidateSupplier()) return;

        try
        {
            using var db = new WarehouseContext();
            var entity = db.Suppliers.Find(SelectedSupplier.Id);
            if (entity is null)
            {
                DialogService.Error("Доставчикът вече не съществува в базата данни.");
                LoadData();
                return;
            }

            entity.Name = SupplierName.Trim();
            entity.Phone = SupplierPhone.Trim();
            entity.Email = SupplierEmail.Trim();
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            DialogService.Error("Грешка при редакция на доставчика:\n" + ex.Message);
            return;
        }

        ClearSupplier();
        LoadData();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSupplier))]
    private void DeleteSupplier()
    {
        if (SelectedSupplier is null) return;

        try
        {
            using var db = new WarehouseContext();
            var count = db.Products.Count(p => p.SupplierId == SelectedSupplier.Id);
            if (count > 0)
            {
                DialogService.Warning(
                    $"Доставчикът „{SelectedSupplier.Name}“ не може да бъде изтрит, " +
                    $"защото към него има обвързани артикули: {count}.\n\n" +
                    "Първо изтрийте или променете доставчика на тези артикули.");
                return;
            }

            if (!DialogService.Confirm($"Сигурни ли сте, че искате да изтриете доставчик „{SelectedSupplier.Name}“?"))
                return;

            var entity = db.Suppliers.Find(SelectedSupplier.Id);
            if (entity is not null)
            {
                db.Suppliers.Remove(entity);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            DialogService.Error("Грешка при изтриване:\n" + ex.Message);
            return;
        }

        ClearSupplier();
        LoadData();
    }

    [RelayCommand]
    private void ClearSupplier()
    {
        SelectedSupplier = null;
        SupplierName = string.Empty;
        SupplierPhone = string.Empty;
        SupplierEmail = string.Empty;
    }
}
