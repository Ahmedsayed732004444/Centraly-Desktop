using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Suppliers;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace Centraly.Desktop.Views;

public partial class SuppliersPage : Page
{
    private readonly IServiceRunner _runner;
    private readonly IServiceProvider _services;

    public SuppliersPage(IServiceRunner runner, IServiceProvider services)
    {
        InitializeComponent();
        _runner = runner;
        _services = services;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _runner.RunAsync<ISupplierService, Result<PaginatedList<SupplierResponse>>>(
            svc => svc.GetAllSuppliersAsync(new RequestFilters { PageSize = 100 }));

        SuppliersGrid.ItemsSource = result.IsSuccess ? result.Value.Items : [];
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        var window = new AddSupplierWindow(_runner) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (SuppliersGrid.SelectedItem is not SupplierResponse supplier)
        {
            MessageBox.Show("اختر مورداً أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new AddSupplierWindow(_runner, supplier) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (SuppliersGrid.SelectedItem is not SupplierResponse supplier)
        {
            MessageBox.Show("اختر مورداً أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"حذف المورد \"{supplier.Name}\"؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await _runner.RunAsync<ISupplierService, Result<bool>>(svc => svc.DeleteSupplierAsync(supplier.SupplierId));
        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadAsync();
    }
}
