using HCIDE.Models;
using System;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Сервис для работы с интерпретаторами/компиляторами
    /// </summary>
    public interface IInterpreterService
    {
        /// <summary>
        /// Найти интерпретатор в системе
        /// </summary>
        Task<string?> FindInterpreterAsync(ProjectType type);

        /// <summary>
        /// Скачать и установить интерпретатор
        /// </summary>
        Task<bool> DownloadAndInstallAsync(
            ProjectType type,
            string targetPath,
            IProgress<double> progress,
            IProgress<string> statusProgress
        );

        /// <summary>
        /// Проверить версию интерпретатора
        /// </summary>
        Task<string?> GetVersionAsync(ProjectType type, string interpreterPath);

        /// <summary>
        /// Проверить доступность интерпретатора
        /// </summary>
        Task<bool> ValidateInterpreterAsync(string interpreterPath);
    }
}