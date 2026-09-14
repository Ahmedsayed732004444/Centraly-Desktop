using Centraly.Api.Contracts.Inventory.Products;
using Wpf.Ui.Controls;

namespace Centraly.Desktop.Views;

public partial class BatchPickerWindow : FluentWindow
{
    private sealed record BatchOption(
        ProductBatchResponse Batch, string SupplierLabel, string DateLabel, string WholesaleLabel, string RetailLabel);

    public ProductBatchResponse? SelectedBatch { get; private set; }
    public decimal UnitPrice { get; private set; }

    public BatchPickerWindow(ProductResponse product)
    {
        InitializeComponent();

        ProductNameText.Text = product.Name;
        BatchesList.ItemsSource = product.Batches
            .Where(b => b.AvailableQuantity > 0)
            .Select(b => new BatchOption(
                b,
                string.IsNullOrWhiteSpace(b.SupplierName) ? "بدون مورد" : b.SupplierName,
                $"تاريخ: {b.DateReceived:yyyy-MM-dd} • متاح: {b.AvailableQuantity}",
                $"جملة {b.WholesalePrice:0.##} ج.م.",
                $"تجزئة {b.RetailPrice:0.##} ج.م."))
            .ToList();
    }

    private void OnWholesaleClick(object sender, RoutedEventArgs e) => Choose(sender, useWholesale: true);

    private void OnRetailClick(object sender, RoutedEventArgs e) => Choose(sender, useWholesale: false);

    private void Choose(object sender, bool useWholesale)
    {
        if (sender is not FrameworkElement { Tag: BatchOption option })
            return;

        SelectedBatch = option.Batch;
        UnitPrice = useWholesale ? option.Batch.WholesalePrice : option.Batch.RetailPrice;

        DialogResult = true;
        Close();
    }
}
