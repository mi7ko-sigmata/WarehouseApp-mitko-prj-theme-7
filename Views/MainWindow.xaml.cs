using System.Windows;
using WarehouseApp.ViewModels;

namespace WarehouseApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
