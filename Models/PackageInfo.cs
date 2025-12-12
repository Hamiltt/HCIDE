namespace HCIDE.Models
{
    /// <summary>
    /// Информация о пакете/библиотеке (для Package Manager)
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// Название пакета
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Установленная версия
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Последняя доступная версия
        /// </summary>
        public string? LatestVersion { get; set; }

        /// <summary>
        /// Описание пакета
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Автор пакета
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Лицензия
        /// </summary>
        public string? License { get; set; }

        /// <summary>
        /// Установлен ли пакет
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Требуется ли обновление
        /// </summary>
        public bool NeedsUpdate => IsInstalled &&
                                   !string.IsNullOrEmpty(LatestVersion) &&
                                   Version != LatestVersion;
    }
}