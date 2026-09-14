using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Departments;
using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Api.Contracts.Sales;
using Centraly.Api.Contracts.Shared.Enums;
using System.Collections.ObjectModel;

namespace Centraly.Desktop.ViewModels;

public partial class PosViewModel(IServiceRunner runner, ICurrentUser currentUser) : ObservableObject
{
    private const int PageSize = 8;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DepartmentResponse? _selectedDepartment;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    public ObservableCollection<DepartmentResponse> Departments { get; } = [];
    public ObservableCollection<ProductResponse> SearchResults { get; } = [];
    public ObservableCollection<CartLineViewModel> Cart { get; } = [];

    // Mirrors sales/utils/cartLogic.ts::cartTotal.
    public decimal CartTotal => Cart.Sum(l => l.LineTotal);
    public int CartItemCount => Cart.Count;
    public int CartQuantity => Cart.Sum(l => l.Quantity);

    public async Task LoadDepartmentsAsync()
    {
        var result = await runner.RunAsync<IDepartmentService, Result<PaginatedList<DepartmentResponse>>>(
            svc => svc.GetAllDepartmentsAsync(new RequestFilters { PageSize = 100 }));

        Departments.Clear();
        if (result.IsSuccess)
            foreach (var d in result.Value.Items)
                Departments.Add(d);

        await SearchAsync();
    }

    async partial void OnSelectedDepartmentChanged(DepartmentResponse? value)
    {
        PageNumber = 1;
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsSearching = true;
        try
        {
            var filters = new RequestFilters
            {
                SearchValue = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                DepartmentId = SelectedDepartment?.DepartmentId,
                PageNumber = PageNumber,
                PageSize = PageSize,
                ExcludeUsage = ProductUsageDto.MaintenanceOnly,
            };
            var result = await runner.RunAsync<IProductService, Result<PaginatedList<ProductResponse>>>(
                svc => svc.GetAllProductsAsync(filters));

            SearchResults.Clear();
            if (result.IsSuccess)
            {
                foreach (var product in result.Value.Items)
                    SearchResults.Add(product);
                TotalPages = Math.Max(1, result.Value.TotalPages);
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber >= TotalPages)
            return;
        PageNumber++;
        await SearchAsync();
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (PageNumber <= 1)
            return;
        PageNumber--;
        await SearchAsync();
    }

    // Mirrors cartLogic.ts::addOrIncrementCartItem - same (productId, batchId) increments
    // quantity instead of duplicating the row.
    public void AddToCart(ProductResponse product, ProductBatchResponse batch, decimal unitPrice)
    {
        var existing = Cart.FirstOrDefault(l => l.ProductId == product.ProductId && l.BatchId == batch.BatchId);
        if (existing is not null)
        {
            if (existing.Quantity < existing.AvailableQuantity)
                existing.Quantity++;
        }
        else
        {
            Cart.Add(new CartLineViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.Name ?? product.Barcode ?? product.ProductId,
                BatchId = batch.BatchId,
                AvailableQuantity = batch.AvailableQuantity,
                ImageUrl = product.ImageUrl,
                SupplierName = batch.SupplierName,
                Quantity = 1,
                UnitPrice = unitPrice,
            });
        }
        RaiseCartChanged();
    }

    [RelayCommand]
    private void RemoveLine(CartLineViewModel line)
    {
        Cart.Remove(line);
        RaiseCartChanged();
    }

    // Mirrors cartLogic.ts::updateCartQuantity - delta below 1 removes the line, above the
    // batch's available quantity is a no-op.
    [RelayCommand]
    private void IncrementLine(CartLineViewModel line)
    {
        if (line.Quantity < line.AvailableQuantity)
            line.Quantity++;
        RaiseCartChanged();
    }

    [RelayCommand]
    private void DecrementLine(CartLineViewModel line)
    {
        if (line.Quantity <= 1)
            Cart.Remove(line);
        else
            line.Quantity--;
        RaiseCartChanged();
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        RaiseCartChanged();
    }

    private void RaiseCartChanged()
    {
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(CartItemCount));
        OnPropertyChanged(nameof(CartQuantity));
    }

    // Called by CheckoutWindow after collecting sale type / customer info. Payment method is
    // fixed per window instance (which button on the POS page opened it).
    public async Task<Result<SalesInvoiceResponse>> SubmitSaleAsync(
        PaymentMethodDto paymentMethod, SaleTypeDto saleType, string? customerName, string? customerPhone, decimal paidAmount)
    {
        var request = new CreateSalesInvoiceRequest(
            CustomerId: null,
            CustomerName: string.IsNullOrWhiteSpace(customerName) ? null : customerName,
            CustomerPhone: string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone,
            SaleType: saleType,
            PaymentMethod: paymentMethod,
            PaidAmount: paidAmount,
            Notes: null,
            Items: Cart.Select(l => new CreateSalesInvoiceItemRequest(l.ProductId, l.BatchId, l.Quantity, l.UnitPrice)).ToList(),
            PaymentSource: null);

        var result = await runner.RunAsync<ISalesInvoiceService, Result<SalesInvoiceResponse>>(
            svc => svc.AddInvoiceAsync(request, currentUser.UserId));

        if (result.IsSuccess)
        {
            Cart.Clear();
            RaiseCartChanged();
            await SearchAsync();
        }

        return result;
    }
}
