using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Products;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace Centraly.Desktop.Views;

public partial class ProductsPage : Page
{
    private readonly IServiceRunner _runner;
    private readonly IServiceProvider _services;

    public ProductsPage(IServiceRunner runner, IServiceProvider services)
    {
        InitializeComponent();
        _runner = runner;
        _services = services;
        Loaded += async (_, _) => await SearchAsync();
    }

    private async Task SearchAsync()
    {
        var filters = new RequestFilters { SearchValue = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text, PageSize = 50 };
        var result = await _runner.RunAsync<IProductService, Result<PaginatedList<ProductResponse>>>(
            svc => svc.GetAllProductsAsync(filters));

        var items = result.IsSuccess ? result.Value.Items : [];
        ProductsGrid.ItemsSource = items;
        EmptyStateText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnSearchClick(object sender, RoutedEventArgs e) => await SearchAsync();

    private async void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await SearchAsync();
    }

    private async void OnAddProductClick(object sender, RoutedEventArgs e)
    {
        var window = _services.GetRequiredService<Views.AddProductWindow>();
        window.Owner = Window.GetWindow(this);
        if (window.ShowDialog() == true)
            await SearchAsync();
    }

    private async void OnDeleteProductClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ProductResponse product })
            return;

        if (MessageBox.Show($"حذف المنتج \"{product.Name}\"؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await _runner.RunAsync<IProductService, Result<bool>>(svc => svc.DeleteProductAsync(product.ProductId));
        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await SearchAsync();
    }
}
