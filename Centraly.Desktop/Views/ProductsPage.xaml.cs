using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Products;

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

        ProductsGrid.ItemsSource = result.IsSuccess ? result.Value.Items : [];
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
}
