using Centraly.Desktop.Services;
using Centraly.Desktop.Views;
using Wpf.Ui.Controls;

namespace Centraly.Desktop;

public partial class MainWindow : FluentWindow
{
    private readonly ICurrentUser _currentUser;

    // Mirrors Sidebar.tsx 1:1 - same groups, same labels, same order, same role gates.
    // Every item currently opens a placeholder; Phases B-K replace the placeholder with
    // the real page one module at a time.
    private static readonly (string Label, string[] Roles)[] Pinned =
    [
        ("الرئيسية", ["Admin", "Manager", "Salesperson", "Technician"]),
    ];

    private static readonly (string Group, (string Label, string[] Roles)[] Items)[] Groups =
    [
        ("الوصول السريع", [
            ("شاشة الكاشير", ["Admin", "Manager", "Salesperson"]),
            ("عمليات المحافظ", ["Admin", "Manager"]),
        ]),
        ("الصيانة", [
            ("تذاكر الصيانة", ["Admin", "Manager", "Technician"]),
        ]),
        ("المبيعات", [
            ("سجل المبيعات", ["Admin", "Manager", "Salesperson"]),
            ("مرتجعات المبيعات", ["Admin", "Manager", "Salesperson"]),
        ]),
        ("المشتريات", [
            ("فاتورة مشتريات", ["Admin", "Manager"]),
            ("سجل المشتريات", ["Admin", "Manager"]),
            ("مرتجعات الموردين", ["Admin", "Manager"]),
        ]),
        ("المخزون", [
            ("المنتجات", ["Admin", "Manager", "Salesperson", "Technician"]),
            ("التصنيفات", ["Admin", "Manager", "Salesperson", "Technician"]),
        ]),
        ("جهات الاتصال", [
            ("العملاء", ["Admin", "Manager", "Salesperson"]),
            ("الموردين", ["Admin", "Manager"]),
        ]),
        ("الماليات", [
            ("الدرج والمصروفات", ["Admin", "Manager", "Salesperson", "Technician"]),
            ("الخزينات", ["Admin", "Manager", "Salesperson", "Technician"]),
            ("المصروفات", ["Admin", "Manager", "Salesperson", "Technician"]),
            ("معاملات المالك", ["Admin", "Manager", "Salesperson", "Technician"]),
        ]),
        ("التحليلات", [
            ("لوحة التحليلات", ["Admin", "Manager"]),
        ]),
        ("الإدارة والصلاحيات", [
            ("إدارة المستخدمين", ["Admin", "Manager"]),
            ("الأدوار والصلاحيات", ["Admin", "Manager"]),
        ]),
    ];

    private static readonly (string Label, string[] Roles)[] Footer =
    [
        ("سياسات النظام", ["Admin", "Manager", "Salesperson", "Technician"]),
        ("إدارة المحافظ", ["Admin", "Manager"]),
    ];

    public MainWindow(ICurrentUser currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;

        BuildMenu();
        PageHost.Content = new ComingSoonPage("الرئيسية");
    }

    private void BuildMenu()
    {
        foreach (var (label, roles) in Pinned)
        {
            if (_currentUser.HasAnyRole(roles))
                MenuPanel.Children.Add(CreateItemButton(label));
        }

        foreach (var (group, items) in Groups)
        {
            var visibleItems = items.Where(i => _currentUser.HasAnyRole(i.Roles)).ToList();
            if (visibleItems.Count == 0)
                continue;

            var header = new System.Windows.Controls.TextBlock
            {
                Text = group,
                Style = (Style)FindResource("SidebarGroupHeaderStyle"),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            MenuPanel.Children.Add(header);

            foreach (var (label, _) in visibleItems)
                MenuPanel.Children.Add(CreateItemButton(label));
        }

        foreach (var (label, roles) in Footer)
        {
            if (_currentUser.HasAnyRole(roles))
                MenuPanel.Children.Add(CreateItemButton(label));
        }
    }

    private System.Windows.Controls.Button CreateItemButton(string label)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = label,
            Style = (Style)FindResource("SidebarItemStyle"),
        };
        button.Click += (_, _) => PageHost.Content = new ComingSoonPage(label);
        return button;
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        _currentUser.SignOut();

        var loginWindow = App.AppHost.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        Close();
    }
}
