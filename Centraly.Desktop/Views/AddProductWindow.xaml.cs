using Centraly.Desktop.ViewModels;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace Centraly.Desktop.Views;

public partial class AddProductWindow : FluentWindow
{
    private readonly AddProductViewModel _viewModel;

    public bool ProductSaved => _viewModel.Saved;

    public AddProductWindow(AddProductViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadDepartmentsAsync();
    }

    private void OnPickImageClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "صور|*.jpg;*.jpeg;*.png;*.webp" };
        if (dialog.ShowDialog() == true)
            _viewModel.ImagePath = dialog.FileName;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveCommand.ExecuteAsync(null);
        if (_viewModel.Saved)
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
