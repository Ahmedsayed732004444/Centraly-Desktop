using Centraly.Api.Contracts.Suppliers;
using Wpf.Ui.Controls;

namespace Centraly.Desktop.Views;

public partial class AddSupplierWindow : FluentWindow
{
    private readonly IServiceRunner _runner;
    private readonly string? _editingSupplierId;

    public bool Saved { get; private set; }

    public AddSupplierWindow(IServiceRunner runner, SupplierResponse? existing = null)
    {
        InitializeComponent();
        _runner = runner;

        if (existing is not null)
        {
            _editingSupplierId = existing.SupplierId;
            Title = "تعديل المورد";
            NameBox.Text = existing.Name;
            TypeBox.Text = existing.Type ?? string.Empty;
            PhoneBox.Text = existing.Phone ?? string.Empty;
            AddressBox.Text = existing.Address ?? string.Empty;
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "اسم المورد مطلوب";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var type = string.IsNullOrWhiteSpace(TypeBox.Text) ? null : TypeBox.Text;
        var phone = string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text;
        var address = string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text;

        var result = _editingSupplierId is null
            ? await _runner.RunAsync<ISupplierService, Result<SupplierResponse>>(
                svc => svc.AddSupplierAsync(new CreateSupplierRequest(NameBox.Text, type, phone, address), null))
            : await _runner.RunAsync<ISupplierService, Result<SupplierResponse>>(
                svc => svc.UpdateSupplierAsync(_editingSupplierId, new UpdateSupplierRequest(NameBox.Text, type, phone, address), null));

        if (result.IsFailure)
        {
            ErrorText.Text = result.Error.Description;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        Saved = true;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
