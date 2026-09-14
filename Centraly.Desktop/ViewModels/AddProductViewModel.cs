using Centraly.Api.Contracts.Common;
using Centraly.Api.Contracts.Inventory.Categories;
using Centraly.Api.Contracts.Inventory.Departments;
using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Api.Contracts.Shared.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.ObjectModel;

namespace Centraly.Desktop.ViewModels;

public partial class AddProductViewModel(IServiceRunner runner, ICurrentUser currentUser) : ObservableObject
{
    [ObservableProperty]
    private string _barcode = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _minQuantityAlert = 5;

    [ObservableProperty]
    private string _storageLocation = string.Empty;

    [ObservableProperty]
    private ProductUsageDto _selectedUsage = ProductUsageDto.SaleOnly;

    [ObservableProperty]
    private DepartmentResponse? _selectedDepartment;

    [ObservableProperty]
    private CategoryResponse? _selectedCategory;

    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSaving;

    public ObservableCollection<DepartmentResponse> Departments { get; } = [];
    public ObservableCollection<CategoryResponse> Categories { get; } = [];
    public ObservableCollection<ProductPropertyRow> Properties { get; } = [];

    public List<ProductUsageDto> UsageOptions { get; } =
        [ProductUsageDto.SaleOnly, ProductUsageDto.MaintenanceOnly, ProductUsageDto.SaleAndMaintenance];

    public bool Saved { get; private set; }

    public async Task LoadDepartmentsAsync()
    {
        var result = await runner.RunAsync<IDepartmentService, Result<PaginatedList<DepartmentResponse>>>(
            svc => svc.GetAllDepartmentsAsync(new RequestFilters { PageSize = 100 }));

        Departments.Clear();
        if (result.IsSuccess)
            foreach (var d in result.Value.Items)
                Departments.Add(d);
    }

    async partial void OnSelectedDepartmentChanged(DepartmentResponse? value)
    {
        Categories.Clear();
        SelectedCategory = null;
        if (value is null)
            return;

        var result = await runner.RunAsync<ICategoryService, Result<PaginatedList<CategoryResponse>>>(
            svc => svc.GetAllCategoriesAsync(new RequestFilters { DepartmentId = value.DepartmentId, PageSize = 100 }));

        if (result.IsSuccess)
            foreach (var c in result.Value.Items)
                Categories.Add(c);
    }

    [RelayCommand]
    private void GenerateBarcode() => Barcode = Random.Shared.NextInt64(1_000_000_000_000, 9_999_999_999_999).ToString();

    [RelayCommand]
    private void AddProperty() => Properties.Add(new ProductPropertyRow());

    [RelayCommand]
    private void RemoveProperty(ProductPropertyRow row) => Properties.Remove(row);

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "اسم المنتج مطلوب";
            return;
        }
        if (SelectedDepartment is null || SelectedCategory is null)
        {
            ErrorMessage = "اختر القسم والتصنيف";
            return;
        }

        IsSaving = true;
        try
        {
            IFormFile? image = null;
            FileStream? stream = null;

            if (!string.IsNullOrEmpty(ImagePath))
            {
                stream = new FileStream(ImagePath, FileMode.Open, FileAccess.Read);
                image = new FormFile(stream, 0, stream.Length, "image", Path.GetFileName(ImagePath));
            }

            try
            {
                var properties = Properties
                    .Where(p => !string.IsNullOrWhiteSpace(p.Key))
                    .ToDictionary(p => p.Key, p => p.Value);

                var request = new CreateProductRequest(
                    string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                    Name,
                    SelectedDepartment.DepartmentId,
                    SelectedCategory.CategoryId,
                    image,
                    MinQuantityAlert,
                    string.IsNullOrWhiteSpace(StorageLocation) ? null : StorageLocation,
                    SelectedUsage,
                    properties.Count == 0 ? null : properties);

                var result = await runner.RunAsync<IProductService, Result<ProductResponse>>(
                    svc => svc.AddProductAsync(request, currentUser.UserId));

                if (result.IsFailure)
                {
                    ErrorMessage = result.Error.Description;
                    return;
                }

                Saved = true;
            }
            finally
            {
                stream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر حفظ المنتج: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
