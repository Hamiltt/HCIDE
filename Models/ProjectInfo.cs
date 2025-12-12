using HCIDE.Models;
using System;
using System.Collections.Generic;

namespace HCIDE.Models
{
    /// <summary>
    /// Типы поддерживаемых проектов
    /// </summary>
    public enum ProjectType
    {
        Python312,
        JavaScript,
        Go121
    }

    /// <summary>
    /// Основная информация о проекте
    /// </summary>
    public class ProjectInfo
    {
        /// <summary>
        /// Название проекта
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Полный путь к папке проекта
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Тип проекта (Python, JavaScript, Go)
        /// </summary>
        public ProjectType Type { get; set; }

        /// <summary>
        /// Путь к интерпретатору/компилятору
        /// </summary>
        public string InterpreterPath { get; set; } = string.Empty;

        /// <summary>
        /// Дата последнего открытия проекта
        /// </summary>
        public DateTime LastOpened { get; set; }

        /// <summary>
        /// Список путей к открытым файлам
        /// </summary>
        public List<string> OpenFiles { get; set; } = new();

        /// <summary>
        /// Путь к активному (выбранному) файлу
        /// </summary>
        public string ActiveFile { get; set; } = string.Empty;

        /// <summary>
        /// Настройки темы для этого проекта
        /// </summary>
        public ThemeSettings? Theme { get; set; }

        /// <summary>
        /// Дополнительные метаданные проекта
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}