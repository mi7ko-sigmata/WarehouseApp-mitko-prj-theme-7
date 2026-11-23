using System.Windows;

namespace WarehouseApp.Services;

public static class DialogService
{
    private const string Title = "Складов контрол";

    public static void Info(string message) =>
        MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Warning(string message) =>
        MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void Error(string message) =>
        MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Error);

    public static bool Confirm(string message) =>
        MessageBox.Show(message, Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
