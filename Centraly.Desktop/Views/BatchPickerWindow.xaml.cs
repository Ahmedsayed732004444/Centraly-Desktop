using Centraly.Api.Contracts.Inventory.Products;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace Centraly.Desktop.Views;

public partial class BatchPickerWindow : FluentWindow
{
    private sealed record BatchOption(ProductBatchResponse Batch, string Display);

    public ProductBatchResponse? SelectedBatch { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public BatchPickerWindow(ProductResponse product)
    {
        InitializeComponent();

        ProductNameText.Text = product.Name;
        BatchesList.ItemsSource = product.Batches
            .Select(b => new BatchOption(
                b,
                $"{(string.IsNullOrWhiteSpace(b.SupplierName) ? "بدون مورد" : b.SupplierName)} - متاح: {b.AvailableQuantity} - تجزئة: {b.RetailPrice:0.##} - جملة: {b.WholesalePrice:0.##}"))
            .ToList();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (BatchesList.SelectedItem is not BatchOption option)
        {
            MessageBox.Show("اختر دفعة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("الكمية غير صالحة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (quantity > option.Batch.AvailableQuantity)
        {
            MessageBox.Show("الكمية المطلوبة أكبر من المتاح في هذه الدفعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedBatch = option.Batch;
        Quantity = quantity;
        UnitPrice = PriceTypeCombo.SelectedIndex == 1 ? option.Batch.WholesalePrice : option.Batch.RetailPrice;

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
