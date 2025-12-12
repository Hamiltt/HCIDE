using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCIDE.Models;
using HCIDE.Services;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace HCIDE.ViewModels
{
    /// <summary>
    /// ViewModel для окна настроек
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IThemeService _themeService;
        private readonly string? _projectPath;

        [ObservableProperty]
        private ThemeSettings themeSettings;

        public event EventHandler<ThemeSettings>? ThemeApplied;

        public SettingsViewModel(IThemeService themeService, string? projectPath = null)
        {
            _themeService = themeService;
            _projectPath = projectPath;
            themeSettings = _themeService.GetCurrentTheme().Clone();
        }

        [RelayCommand]
        private void RandomizeColors()
        {
            ThemeSettings = ThemeSettings.CreateRandomized();
            Log.Information("Theme colors randomized");
        }

        [RelayCommand]
        private async Task ResetToDefault()
        {
            var result = System.Windows.MessageBox.Show(
                "Reset theme to default values?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                ThemeSettings = ThemeSettings.CreateDefault();
                await _themeService.SaveThemeAsync(ThemeSettings, _projectPath);
                _themeService.ApplyTheme(ThemeSettings);
                ThemeApplied?.Invoke(this, ThemeSettings);

                Log.Information("Theme reset to default");
                System.Windows.MessageBox.Show("Theme reset successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void ApplyTheme()
        {
            _themeService.ApplyTheme(ThemeSettings);
            ThemeApplied?.Invoke(this, ThemeSettings);
            Log.Information("Theme applied");
        }

        [RelayCommand]
        private async Task SaveTheme()
        {
            try
            {
                await _themeService.SaveThemeAsync(ThemeSettings, _projectPath);
                _themeService.ApplyTheme(ThemeSettings);
                ThemeApplied?.Invoke(this, ThemeSettings);

                Log.Information("Theme saved");
                System.Windows.MessageBox.Show("Theme saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving theme");
                System.Windows.MessageBox.Show($"Error saving theme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void PickColor(string propertyName)
        {
            // Открываем ColorPicker dialog
            var dialog = new System.Windows.Forms.ColorDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = dialog.Color;
                var hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                // Устанавливаем цвет в соответствующее свойство
                var property = typeof(ThemeSettings).GetProperty(propertyName);
                property?.SetValue(ThemeSettings, hexColor);

                // Уведомляем об изменении
                OnPropertyChanged(nameof(ThemeSettings));
            }
        }
    }
}