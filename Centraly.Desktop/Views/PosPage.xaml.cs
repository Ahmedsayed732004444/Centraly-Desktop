using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Desktop.ViewModels;

namespace Centraly.Desktop.Views;

public partial class PosPage : Page
{
    private readonly PosViewModel _viewModel;

    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _viewModel.SearchCommand.ExecuteAsync(null);
    }

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsList.SelectedItem is not ProductResponse product)
            return;

        if (product.Batches.Count == 0)
        {
            MessageBox.Show("لا توجد دفعات متاحة لهذا المنتج", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var picker = new BatchPickerWindow(product) { Owner = Window.GetWindow(this) };
        if (picker.ShowDialog() == true && picker.SelectedBatch is not null)
        {
            _viewModel.AddToCart(product, picker.SelectedBatch, picker.Quantity, picker.UnitPrice);
        }
    }
}
