using System;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using WarehouseApp.Data;

namespace WarehouseApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("Възникна грешка:\n" + args.Exception.Message,
                "Складов контрол", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
        catch (ArgumentException)
        {
            // метаданните вече са зададени - продължаваме
        }

        try
        {
            DbInitializer.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Грешка при инициализиране на базата данни:\n" + ex.Message,
                "Складов контрол", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }
}