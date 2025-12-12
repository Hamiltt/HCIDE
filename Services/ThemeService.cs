using HCIDE.Models;
using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HCIDE.Services
{
    /// <summary>
    /// Реализация сервиса для работы с темами
    /// </summary>
    public class ThemeService : IThemeService
    {
        private ThemeSettings _currentTheme;
        private readonly string _appDataPath;
        private readonly string _globalThemePath;

        public ThemeService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "YourIDE"
            );
            Directory.CreateDirectory(_appDataPath);
            _globalThemePath = Path.Combine(_appDataPath, "theme.json");

            // Загружаем тему по умолчанию
            _currentTheme = ThemeSettings.CreateDefault();
        }

        public async Task<ThemeSettings> LoadThemeAsync(string? projectPath = null)
        {
            try
            {
                string themePath;

                // Определяем путь: проектная тема или глобальная
                if (!string.IsNullOrEmpty(projectPath))
                {
                    themePath = Path.Combine(projectPath, ".youride", "theme.json");

                    // Если проектной темы нет, загружаем глобальную
                    if (!File.Exists(themePath))
                    {
                        themePath = _globalThemePath;
                    }
                }
                else
                {
                    themePath = _globalThemePath;
                }

                // Загружаем тему из файла
                if (File.Exists(themePath))
                {
                    var json = await File.ReadAllTextAsync(themePath);
                    var theme = JsonSerializer.Deserialize<ThemeSettings>(json);

                    if (theme != null)
                    {
                        _currentTheme = theme;
                        Log.Information("Theme loaded from: {Path}", themePath);
                        return theme;
                    }
                }

                // Если файл не найден, возвращаем тему по умолчанию
                Log.Information("Using default theme");
                _currentTheme = ThemeSettings.CreateDefault();
                return _currentTheme;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading theme");
                return ThemeSettings.CreateDefault();
            }
        }

        public async Task SaveThemeAsync(ThemeSettings theme, string? projectPath = null)
        {
            try
            {
                string themePath;

                // Определяем путь сохранения
                if (!string.IsNullOrEmpty(projectPath))
                {
                    var yourIdePath = Path.Combine(projectPath, ".youride");
                    Directory.CreateDirectory(yourIdePath);
                    themePath = Path.Combine(yourIdePath, "theme.json");
                }
                else
                {
                    themePath = _globalThemePath;
                }

                // Сериализуем и сохраняем
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(theme, options);
                await File.WriteAllTextAsync(themePath, json);

                _currentTheme = theme;
                Log.Information("Theme saved to: {Path}", themePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving theme");
                throw;
            }
        }

        public ThemeSettings GetCurrentTheme()
        {
            return _currentTheme;
        }

        public void ApplyTheme(ThemeSettings theme)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app == null)
                {
                    Log.Warning("Application.Current is null, cannot apply theme");
                    return;
                }

                // Применяем цвета к ресурсам приложения
                UpdateResource(app, "EditorBackground", theme.EditorBackground);
                UpdateResource(app, "EditorForeground", theme.EditorForeground);
                UpdateResource(app, "LineNumberForeground", theme.LineNumberForeground);
                UpdateResource(app, "SelectionBackground", theme.SelectionBackground);
                UpdateResource(app, "CaretColor", theme.CaretColor);
                UpdateResource(app, "KeywordColor", theme.KeywordColor);
                UpdateResource(app, "StringColor", theme.StringColor);
                UpdateResource(app, "CommentColor", theme.CommentColor);
                UpdateResource(app, "NumberColor", theme.NumberColor);
                UpdateResource(app, "FunctionColor", theme.FunctionColor);
                UpdateResource(app, "ClassColor", theme.ClassColor);
                UpdateResource(app, "VariableColor", theme.VariableColor);
                UpdateResource(app, "OperatorColor", theme.OperatorColor);
                UpdateResource(app, "PanelBackground", theme.PanelBackground);
                UpdateResource(app, "BorderColor", theme.BorderColor);
                UpdateResource(app, "StatusBarBackground", theme.StatusBarBackground);

                _currentTheme = theme;
                Log.Information("Theme applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying theme");
            }
        }

        private void UpdateResource(System.Windows.Application app, string key, string colorHex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
                var brush = new SolidColorBrush(color);
                brush.Freeze(); // Оптимизация для WPF

                if (app.Resources.Contains(key))
                {
                    app.Resources[key] = brush;
                }
                else
                {
                    app.Resources.Add(key, brush);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error updating resource {Key} with color {Color}", key, colorHex);
            }
        }

        public async Task ResetToDefaultAsync(string? projectPath = null)
        {
            var defaultTheme = ThemeSettings.CreateDefault();
            await SaveThemeAsync(defaultTheme, projectPath);
            ApplyTheme(defaultTheme);
            Log.Information("Theme reset to default");
        }

        public async Task<bool> ExportThemeAsync(ThemeSettings theme, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(theme, options);
                await File.WriteAllTextAsync(filePath, json);

                Log.Information("Theme exported to: {Path}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting theme to {Path}", filePath);
                return false;
            }
        }

        public async Task<ThemeSettings?> ImportThemeAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Theme file not found: {filePath}");
                }

                var json = await File.ReadAllTextAsync(filePath);
                var theme = JsonSerializer.Deserialize<ThemeSettings>(json);

                if (theme != null)
                {
                    Log.Information("Theme imported from: {Path}", filePath);
                    return theme;
                }

                Log.Warning("Failed to deserialize theme from: {Path}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing theme from {Path}", filePath);
                return null;
            }
        }
    }
}