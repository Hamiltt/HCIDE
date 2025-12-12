using HCIDE.Services;
using HCIDE.ViewModels;
using HCIDE.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Windows;

namespace HCIDE
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        public IServiceProvider Services => _host!.Services;

        public App()
        {
            // Настройка Serilog для логирования
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HCIDE"
            );
            Directory.CreateDirectory(appDataPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(appDataPath, "log.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            Log.Information("=== HCIDE Starting ===");

            // Настройка Dependency Injection
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Регистрация сервисов
                    services.AddSingleton<IProjectService, ProjectService>();
                    services.AddSingleton<IFileService, FileService>();
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<IInterpreterService, InterpreterService>();
                    services.AddSingleton<IPackageManagerService, PackageManagerService>();

                    // Регистрация ViewModels
                    services.AddTransient<ProjectSelectorViewModel>();
                    services.AddTransient<MainWindowViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // Регистрация Windows
                    services.AddTransient<ProjectSelectorWindow>();
                    services.AddTransient<MainWindow>();
                    services.AddTransient<SettingsWindow>();
                })
                .Build();
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Глобальный обработчик исключений
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                DispatcherUnhandledException += OnDispatcherUnhandledException;

                // Запускаем host
                await _host!.StartAsync();

                // Создаем и показываем стартовое окно
                var startupWindow = _host.Services.GetRequiredService<ProjectSelectorWindow>();
                var viewModel = _host.Services.GetRequiredService<ProjectSelectorViewModel>();
                startupWindow.DataContext = viewModel;
                startupWindow.Show();

                Log.Information("Application started successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application startup failed");
                System.Windows.MessageBox.Show(
                    $"Failed to start application: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown();
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Unhandled exception");

            System.Windows.MessageBox.Show(
                $"A critical error occurred: {exception?.Message}\n\nThe application will close.",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception");

            System.Windows.MessageBox.Show(
                $"An error occurred: {e.Exception.Message}\n\nPlease check the log file for details.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            e.Handled = true; // Предотвращаем падение приложения
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("Application exiting");

                if (_host != null)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                    _host.Dispose();
                }

                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error during application shutdown");
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }

    /// <summary>
    /// Конвертер для инвертирования boolean значений
    /// </summary>
    public class InverseBooleanConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}