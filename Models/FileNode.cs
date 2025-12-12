using System.Collections.Generic;

namespace HCIDE.Models
{
    /// <summary>
    /// Узел дерева файлов (для TreeView в File Explorer)
    /// </summary>
    public class FileNode
    {
        /// <summary>
        /// Имя файла или папки
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Полный путь к файлу/папке
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// Является ли узел директорией
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Расширение файла (для определения иконки)
        /// </summary>
        public string Extension => IsDirectory ? string.Empty : System.IO.Path.GetExtension(Name);

        /// <summary>
        /// Дочерние узлы (файлы и папки внутри)
        /// </summary>
        public List<FileNode> Children { get; set; } = new();

        /// <summary>
        /// Является ли узел развернутым в TreeView
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Выбран ли узел
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Иконка для отображения (emoji или путь к изображению)
        /// </summary>
        public string Icon
        {
            get
            {
                if (IsDirectory) return "📁";

                return Extension.ToLower() switch
                {
                    ".py" => "🐍",
                    ".js" => "📜",
                    ".go" => "🔷",
                    ".json" => "📋",
                    ".xml" => "📄",
                    ".txt" => "📝",
                    ".md" => "📖",
                    ".cs" => "⚙️",
                    _ => "📄"
                };
            }
        }
    }
}