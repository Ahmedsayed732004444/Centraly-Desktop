namespace Centraly.Desktop.ViewModels;

public partial class PurchaseLineViewModel : ObservableObject
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _unitCost;

    [ObservableProperty]
    private decimal _wholesalePrice;

    [ObservableProperty]
    private decimal _retailPrice;

    public decimal LineTotal => Quantity * UnitCost;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitCostChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}
