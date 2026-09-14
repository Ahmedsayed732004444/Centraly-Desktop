namespace Centraly.Desktop.ViewModels;

// Mirrors the old frontend's cart line shape (sales/utils/cartLogic.ts): one line per
// (productId, batchId) pair - adding the same batch again increments quantity instead of
// creating a duplicate row.
public partial class CartLineViewModel : ObservableObject
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string BatchId { get; init; }
    public required int AvailableQuantity { get; init; }
    public string? ImageUrl { get; init; }
    public string? SupplierName { get; init; }

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    public decimal LineTotal => Quantity * UnitPrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}
