using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Categories;
using Centraly.Api.Contracts.Inventory.Departments;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace Centraly.Desktop.Views;

public partial class CategoriesPage : Page
{
    private readonly IServiceRunner _runner;

    public CategoriesPage(IServiceRunner runner)
    {
        InitializeComponent();
        _runner = runner;
        Loaded += async (_, _) => await LoadDepartmentsAsync();
    }

    private async Task LoadDepartmentsAsync()
    {
        var filters = new RequestFilters { PageSize = 100 };
        var result = await _runner.RunAsync<IDepartmentService, Result<PaginatedList<DepartmentResponse>>>(
            svc => svc.GetAllDepartmentsAsync(filters));

        var selectedId = (DepartmentsList.SelectedItem as DepartmentResponse)?.DepartmentId;
        DepartmentsList.ItemsSource = result.IsSuccess ? result.Value.Items : [];
        if (selectedId is not null && result.IsSuccess)
            DepartmentsList.SelectedItem = result.Value.Items.FirstOrDefault(d => d.DepartmentId == selectedId);
    }

    private async Task LoadCategoriesAsync(string departmentId)
    {
        var filters = new RequestFilters { DepartmentId = departmentId, PageSize = 100 };
        var result = await _runner.RunAsync<ICategoryService, Result<PaginatedList<CategoryResponse>>>(
            svc => svc.GetAllCategoriesAsync(filters));

        CategoriesList.ItemsSource = result.IsSuccess ? result.Value.Items : [];
    }

    private async void OnDepartmentSelected(object sender, SelectionChangedEventArgs e)
    {
        if (DepartmentsList.SelectedItem is DepartmentResponse department)
            await LoadCategoriesAsync(department.DepartmentId);
        else
            CategoriesList.ItemsSource = null;
    }

    private async void OnAddDepartmentClick(object sender, RoutedEventArgs e)
    {
        var dialog = new TextInputWindow("قسم جديد", "اسم القسم") { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
            return;

        var result = await _runner.RunAsync<IDepartmentService, Result<DepartmentResponse>>(
            svc => svc.AddDepartmentAsync(new CreateDepartmentRequest(dialog.InputText), null));

        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadDepartmentsAsync();
    }

    private async void OnEditDepartmentClick(object sender, RoutedEventArgs e)
    {
        if (DepartmentsList.SelectedItem is not DepartmentResponse department)
            return;

        var dialog = new TextInputWindow("تعديل القسم", "اسم القسم", department.Name) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
            return;

        var result = await _runner.RunAsync<IDepartmentService, Result<DepartmentResponse>>(
            svc => svc.UpdateDepartmentAsync(department.DepartmentId, new UpdateDepartmentRequest(dialog.InputText), null));

        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadDepartmentsAsync();
    }

    private async void OnDeleteDepartmentClick(object sender, RoutedEventArgs e)
    {
        if (DepartmentsList.SelectedItem is not DepartmentResponse department)
            return;

        if (MessageBox.Show($"حذف القسم \"{department.Name}\"؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await _runner.RunAsync<IDepartmentService, Result<bool>>(
            svc => svc.DeleteDepartmentAsync(department.DepartmentId));

        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadDepartmentsAsync();
    }

    private async void OnAddCategoryClick(object sender, RoutedEventArgs e)
    {
        if (DepartmentsList.SelectedItem is not DepartmentResponse department)
        {
            MessageBox.Show("اختر قسماً أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new TextInputWindow("تصنيف جديد", "اسم التصنيف") { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
            return;

        var result = await _runner.RunAsync<ICategoryService, Result<CategoryResponse>>(
            svc => svc.AddCategoryAsync(new CreateCategoryRequest(dialog.InputText, department.DepartmentId), null));

        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadCategoriesAsync(department.DepartmentId);
    }

    private async void OnEditCategoryClick(object sender, RoutedEventArgs e)
    {
        if (DepartmentsList.SelectedItem is not DepartmentResponse department || CategoriesList.SelectedItem is not CategoryResponse category)
            return;

        var dialog = new TextInputWindow("تعديل التصنيف", "اسم التصنيف", category.Name) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
            return;

        var result = await _runner.RunAsync<ICategoryService, Result<CategoryResponse>>(
            svc => svc.UpdateCategoryAsync(category.CategoryId, new UpdateCategoryRequest(dialog.InputText, department.DepartmentId), null));

        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadCategoriesAsync(department.DepartmentId);
    }

    private async void OnDeleteCategoryClick(object sender, RoutedEventArgs e)
    {
        if (DepartmentsList.SelectedItem is not DepartmentResponse department || CategoriesList.SelectedItem is not CategoryResponse category)
            return;

        if (MessageBox.Show($"حذف التصنيف \"{category.Name}\"؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await _runner.RunAsync<ICategoryService, Result<bool>>(
            svc => svc.DeleteCategoryAsync(category.CategoryId));

        if (result.IsFailure)
            MessageBox.Show(result.Error.Description, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadCategoriesAsync(department.DepartmentId);
    }
}
