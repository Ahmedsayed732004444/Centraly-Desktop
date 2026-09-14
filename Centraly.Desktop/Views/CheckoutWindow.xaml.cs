using Centraly.Api.Abstractions;
using Centraly.Api.Contracts.Sales;
using Centraly.Api.Contracts.Shared.Enums;
using Wpf.Ui.Controls;

namespace Centraly.Desktop.Views;

public partial class CheckoutWindow : FluentWindow
{
    private readonly PaymentMethodDto _paymentMethod;
    private readonly decimal _total;
    private readonly Func<PaymentMethodDto, SaleTypeDto, string?, string?, decimal, Task<Result<SalesInvoiceResponse>>> _submit;

    public bool Completed { get; private set; }

    public CheckoutWindow(
        PaymentMethodDto paymentMethod,
        decimal total,
        Func<PaymentMethodDto, SaleTypeDto, string?, string?, decimal, Task<Result<SalesInvoiceResponse>>> submit)
    {
        InitializeComponent();
        _paymentMethod = paymentMethod;
        _total = total;
        _submit = submit;

        Title = paymentMethod == PaymentMethodDto.Cash ? "إتمام الدفع النقدي" : "إتمام البيع الآجل";
        TitleText.Text = Title;
        TotalText.Text = $"{total:0.##} ج.م.";

        SaleTypeCombo.ItemsSource = new[] { SaleTypeDto.Retail, SaleTypeDto.Wholesale };
        SaleTypeCombo.SelectedIndex = 0;

        if (paymentMethod == PaymentMethodDto.Deferred)
        {
            PaidAmountPanel.Visibility = Visibility.Visible;
            ConfirmButton.Content = "تأكيد وحفظ الفاتورة (آجل)";
            ConfirmButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd9, 0x7f, 0x06));
        }
        else
        {
            ConfirmButton.Background = (System.Windows.Media.Brush)Application.Current.Resources["PosAccentBrush"];
        }
        ConfirmButton.Foreground = System.Windows.Media.Brushes.White;
    }

    private async void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        string? phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text;
        string? name = string.IsNullOrWhiteSpace(CustomerNameBox.Text) ? null : CustomerNameBox.Text;

        if (_paymentMethod == PaymentMethodDto.Deferred && (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(name)))
        {
            ShowError("يجب إدخال اسم العميل ورقم الهاتف في حالة البيع الآجل");
            return;
        }

        var paidAmount = _total;
        if (_paymentMethod == PaymentMethodDto.Deferred)
        {
            if (!decimal.TryParse(PaidAmountBox.Text, out paidAmount) || paidAmount < 0)
            {
                ShowError("يرجى إدخال مبلغ دفع صحيح");
                return;
            }
            // Deferred means *some* balance stays owed - paid must be strictly less than
            // the total (matches CheckoutModal.tsx's own `paid >= totalAmount` rejection).
            if (paidAmount >= _total)
            {
                ShowError("لا يمكن أن يكون المبلغ المدفوع أكبر من أو يساوي الإجمالي في حالة البيع الآجل");
                return;
            }
        }

        var saleType = (SaleTypeDto)SaleTypeCombo.SelectedItem!;

        ConfirmButton.IsEnabled = false;
        try
        {
            var result = await _submit(_paymentMethod, saleType, name, phone, paidAmount);
            if (result.IsFailure)
            {
                ShowError(result.Error.Description);
                return;
            }

            Completed = true;
            DialogResult = true;
            Close();
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
