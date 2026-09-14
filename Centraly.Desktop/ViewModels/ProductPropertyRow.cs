namespace Centraly.Desktop.ViewModels;

public partial class ProductPropertyRow : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}
