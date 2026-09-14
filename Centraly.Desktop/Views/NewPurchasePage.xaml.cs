using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Desktop.ViewModels;

namespace Centraly.Desktop.Views;

public partial class NewPurchasePage : Page
{
    private readonly NewPurchaseViewModel _viewModel;

    public NewPurchasePage(NewPurchaseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadSuppliersAsync();
    }

    private async void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _viewModel.SearchCommand.ExecuteAsync(null);
    }

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsList.SelectedItem is ProductResponse product)
            _viewModel.AddProduct(product);
    }
}
