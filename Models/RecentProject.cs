using System;

namespace HCIDE.Models
{
    /// <summary>
    /// Информация о недавно открытом проекте (для списка в стартовом окне)
    /// </summary>
    public class RecentProject
    {
        /// <summary>
        /// Название проекта
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Полный путь к проекту
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Тип проекта
        /// </summary>
        public ProjectType Type { get; set; }

        /// <summary>
        /// Дата последнего открытия
        /// </summary>
        public DateTime LastOpened { get; set; }

        /// <summary>
        /// Описание проекта (опционально)
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}