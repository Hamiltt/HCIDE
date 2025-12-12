using HCIDE.Models;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Сервис для работы с темами оформления
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Загрузить тему (глобальную или проектную)
        /// </summary>
        Task<ThemeSettings> LoadThemeAsync(string? projectPath = null);

        /// <summary>
        /// Сохранить тему
        /// </summary>
        Task SaveThemeAsync(ThemeSettings theme, string? projectPath = null);

        /// <summary>
        /// Получить текущую активную тему
        /// </summary>
        ThemeSettings GetCurrentTheme();

        /// <summary>
        /// Применить тему к приложению
        /// </summary>
        void ApplyTheme(ThemeSettings theme);

        /// <summary>
        /// Сбросить тему на значения по умолчанию
        /// </summary>
        Task ResetToDefaultAsync(string? projectPath = null);

        /// <summary>
        /// Экспортировать тему в файл
        /// </summary>
        Task<bool> ExportThemeAsync(ThemeSettings theme, string filePath);

        /// <summary>
        /// Импортировать тему из файла
        /// </summary>
        Task<ThemeSettings?> ImportThemeAsync(string filePath);
    }
}