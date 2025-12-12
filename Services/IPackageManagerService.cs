using HCIDE.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Сервис для управления пакетами (pip, npm, go modules)
    /// </summary>
    public interface IPackageManagerService
    {
        /// <summary>
        /// Получить список установленных пакетов
        /// </summary>
        Task<List<PackageInfo>> GetInstalledPackagesAsync(ProjectType type, string projectPath, string interpreterPath);

        /// <summary>
        /// Установить пакет
        /// </summary>
        Task<bool> InstallPackageAsync(
            ProjectType type,
            string packageName,
            string projectPath,
            string interpreterPath,
            IProgress<string> progress
        );

        /// <summary>
        /// Удалить пакет
        /// </summary>
        Task<bool> UninstallPackageAsync(
            ProjectType type,
            string packageName,
            string projectPath,
            string interpreterPath
        );

        /// <summary>
        /// Обновить пакет
        /// </summary>
        Task<bool> UpdatePackageAsync(
            ProjectType type,
            string packageName,
            string projectPath,
            string interpreterPath,
            IProgress<string> progress
        );

        /// <summary>
        /// Поиск пакетов
        /// </summary>
        Task<List<PackageInfo>> SearchPackagesAsync(ProjectType type, string query);
    }
}