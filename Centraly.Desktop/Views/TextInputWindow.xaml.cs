using Wpf.Ui.Controls;

namespace Centraly.Desktop.Views;

public partial class TextInputWindow : FluentWindow
{
    public string InputText { get; private set; } = string.Empty;

    public TextInputWindow(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = initialValue;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputBox.Text))
        {
            System.Windows.MessageBox.Show("من فضلك أدخل قيمة", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        InputText = InputBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
