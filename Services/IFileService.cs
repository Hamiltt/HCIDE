using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Сервис для работы с файлами
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Прочитать содержимое файла
        /// </summary>
        Task<string> ReadFileAsync(string path);

        /// <summary>
        /// Записать содержимое в файл
        /// </summary>
        Task WriteFileAsync(string path, string content);

        /// <summary>
        /// Удалить файл или директорию
        /// </summary>
        Task<bool> DeleteAsync(string path);

        /// <summary>
        /// Переименовать файл или директорию
        /// </summary>
        Task<bool> RenameAsync(string oldPath, string newPath);

        /// <summary>
        /// Создать директорию
        /// </summary>
        Task<bool> CreateDirectoryAsync(string path);

        /// <summary>
        /// Создать пустой файл
        /// </summary>
        Task<bool> CreateFileAsync(string path);

        /// <summary>
        /// Проверить существование файла
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// Проверить существование директории
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Копировать файл
        /// </summary>
        Task<bool> CopyFileAsync(string sourcePath, string destinationPath);
    }
}