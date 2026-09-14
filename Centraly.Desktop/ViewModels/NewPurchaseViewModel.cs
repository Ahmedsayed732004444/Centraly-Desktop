using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Api.Contracts.Suppliers;
using System.Collections.ObjectModel;

namespace Centraly.Desktop.ViewModels;

public partial class NewPurchaseViewModel(IServiceRunner runner, ICurrentUser currentUser) : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private SupplierResponse? _selectedSupplier;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isSubmitting;

    public ObservableCollection<SupplierResponse> Suppliers { get; } = [];
    public ObservableCollection<ProductResponse> SearchResults { get; } = [];
    public ObservableCollection<PurchaseLineViewModel> Items { get; } = [];

    public decimal Total => Items.Sum(i => i.LineTotal);

    public async Task LoadSuppliersAsync()
    {
        var result = await runner.RunAsync<ISupplierService, Result<PaginatedList<SupplierResponse>>>(
            svc => svc.GetAllSuppliersAsync(new RequestFilters { PageSize = 200 }));

        Suppliers.Clear();
        if (result.IsSuccess)
            foreach (var s in result.Value.Items)
                Suppliers.Add(s);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            return;
        }

        var result = await runner.RunAsync<IProductService, Result<PaginatedList<ProductResponse>>>(
            svc => svc.GetAllProductsAsync(new RequestFilters { SearchValue = SearchText, PageSize = 20 }));

        SearchResults.Clear();
        if (result.IsSuccess)
            foreach (var p in result.Value.Items)
                SearchResults.Add(p);
    }

    public void AddProduct(ProductResponse product)
    {
        if (Items.Any(i => i.ProductId == product.ProductId))
            return;

        // Prefills from the product's most recent batch, if any, so re-ordering the same
        // product doesn't require retyping prices that likely haven't changed.
        var lastBatch = product.Batches.FirstOrDefault();

        Items.Add(new PurchaseLineViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.Name ?? product.Barcode ?? product.ProductId,
            UnitCost = lastBatch?.PurchasePrice ?? 0,
            WholesalePrice = lastBatch?.WholesalePrice ?? 0,
            RetailPrice = lastBatch?.RetailPrice ?? 0,
        });
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void RemoveItem(PurchaseLineViewModel line)
    {
        Items.Remove(line);
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedSupplier is null)
        {
            ErrorMessage = "اختر المورد";
            return;
        }
        if (Items.Count == 0)
        {
            ErrorMessage = "يجب إضافة صنف واحد على الأقل";
            return;
        }
        if (PaidAmount > Total)
        {
            ErrorMessage = "المبلغ المدفوع أكبر من إجمالي الفاتورة";
            return;
        }

        IsSubmitting = true;
        try
        {
            var request = new CreatePurchaseInvoiceRequest(
                SelectedSupplier.SupplierId,
                PaidAmount,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                Items.Select(i => new CreatePurchaseInvoiceItemRequest(
                    i.ProductId, i.Quantity, i.UnitCost, i.WholesalePrice, i.RetailPrice)).ToList());

            var result = await runner.RunAsync<IPurchaseInvoiceService, Result<PurchaseInvoiceResponse>>(
                svc => svc.AddPurchaseInvoiceAsync(request, currentUser.UserId));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Description;
                return;
            }

            SuccessMessage = $"تم إنشاء فاتورة المشتريات رقم {result.Value.InvoiceNumber} بنجاح";
            Items.Clear();
            PaidAmount = 0;
            Notes = string.Empty;
            OnPropertyChanged(nameof(Total));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر حفظ الفاتورة: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
