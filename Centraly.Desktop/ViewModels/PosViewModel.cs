using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Api.Contracts.Sales;
using Centraly.Api.Contracts.Shared.Enums;
using System.Collections.ObjectModel;

namespace Centraly.Desktop.ViewModels;

public partial class PosViewModel(IServiceRunner runner, ICurrentUser currentUser) : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isCash = true;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private bool _isSubmitting;

    public ObservableCollection<ProductResponse> SearchResults { get; } = [];
    public ObservableCollection<CartLineViewModel> Cart { get; } = [];

    // Mirrors sales/utils/cartLogic.ts::cartTotal.
    public decimal CartTotal => Cart.Sum(l => l.LineTotal);

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            return;
        }

        IsSearching = true;
        try
        {
            var filters = new RequestFilters { SearchValue = SearchText, PageSize = 20, ExcludeUsage = ProductUsageDto.MaintenanceOnly };
            var result = await runner.RunAsync<IProductService, Result<PaginatedList<ProductResponse>>>(
                svc => svc.GetAllProductsAsync(filters));

            SearchResults.Clear();
            if (result.IsSuccess)
            {
                foreach (var product in result.Value.Items)
                    SearchResults.Add(product);
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    // Mirrors cartLogic.ts::addOrIncrementCartItem - same (productId, batchId) increments
    // quantity instead of duplicating the row.
    public void AddToCart(ProductResponse product, ProductBatchResponse batch, int quantity, decimal unitPrice)
    {
        var existing = Cart.FirstOrDefault(l => l.ProductId == product.ProductId && l.BatchId == batch.BatchId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            Cart.Add(new CartLineViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.Name ?? product.Barcode ?? product.ProductId,
                BatchId = batch.BatchId,
                AvailableQuantity = batch.AvailableQuantity,
                Quantity = quantity,
                UnitPrice = unitPrice,
            });
        }
        OnPropertyChanged(nameof(CartTotal));
    }

    [RelayCommand]
    private void RemoveLine(CartLineViewModel line)
    {
        Cart.Remove(line);
        OnPropertyChanged(nameof(CartTotal));
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (Cart.Count == 0)
        {
            ErrorMessage = "السلة فارغة";
            return;
        }

        var paymentMethod = IsCash ? PaymentMethodDto.Cash : PaymentMethodDto.Deferred;

        if (!IsCash && string.IsNullOrWhiteSpace(CustomerPhone))
        {
            ErrorMessage = "البيع الآجل يحتاج رقم هاتف العميل";
            return;
        }

        var total = CartTotal;
        var paid = IsCash ? total : PaidAmount;

        if (IsCash && paid != total)
            paid = total;

        if (!IsCash && paid > total)
        {
            ErrorMessage = "المبلغ المدفوع أكبر من إجمالي الفاتورة";
            return;
        }

        IsSubmitting = true;
        try
        {
            var request = new CreateSalesInvoiceRequest(
                CustomerId: null,
                CustomerName: string.IsNullOrWhiteSpace(CustomerName) ? null : CustomerName,
                CustomerPhone: string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone,
                SaleType: SaleTypeDto.Retail,
                PaymentMethod: paymentMethod,
                PaidAmount: paid,
                Notes: null,
                Items: Cart.Select(l => new CreateSalesInvoiceItemRequest(l.ProductId, l.BatchId, l.Quantity, l.UnitPrice)).ToList(),
                PaymentSource: null);

            var result = await runner.RunAsync<ISalesInvoiceService, Result<SalesInvoiceResponse>>(
                svc => svc.AddInvoiceAsync(request, currentUser.UserId));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Description;
                return;
            }

            SuccessMessage = $"تم إنشاء الفاتورة رقم {result.Value.InvoiceNumber} بنجاح";
            Cart.Clear();
            CustomerName = string.Empty;
            CustomerPhone = string.Empty;
            PaidAmount = 0;
            OnPropertyChanged(nameof(CartTotal));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر إتمام عملية البيع: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
