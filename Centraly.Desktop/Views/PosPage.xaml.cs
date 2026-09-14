using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Api.Contracts.Shared.Enums;
using Centraly.Desktop.ViewModels;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace Centraly.Desktop.Views;

public partial class PosPage : Page
{
    private readonly PosViewModel _viewModel;

    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadDepartmentsAsync();
    }

    private async void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.PageNumber = 1;
            await _viewModel.SearchCommand.ExecuteAsync(null);
        }
    }

    private void OnAddToCartClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ProductResponse product })
            return;

        if (!product.Batches.Any(b => b.AvailableQuantity > 0))
        {
            MessageBox.Show("لا توجد دفعات متاحة لهذا المنتج في المخزون", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var picker = new BatchPickerWindow(product) { Owner = Window.GetWindow(this) };
        if (picker.ShowDialog() == true && picker.SelectedBatch is not null)
        {
            _viewModel.AddToCart(product, picker.SelectedBatch, picker.UnitPrice);
        }
    }

    private void OnCashClick(object sender, RoutedEventArgs e) => OpenCheckout(PaymentMethodDto.Cash);

    private void OnDeferredClick(object sender, RoutedEventArgs e) => OpenCheckout(PaymentMethodDto.Deferred);

    private void OpenCheckout(PaymentMethodDto method)
    {
        var window = new CheckoutWindow(method, _viewModel.CartTotal, _viewModel.SubmitSaleAsync)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true && window.Completed)
        {
            MessageBox.Show("تم إنشاء الفاتورة بنجاح", "تم البيع", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
