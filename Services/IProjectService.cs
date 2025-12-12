using HCIDE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Сервис для работы с проектами
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Создать новый проект
        /// </summary>
        Task<ProjectInfo> CreateProjectAsync(string name, string path, ProjectType type);

        /// <summary>
        /// Загрузить существующий проект
        /// </summary>
        Task<ProjectInfo> LoadProjectAsync(string path);

        /// <summary>
        /// Сохранить проект
        /// </summary>
        Task SaveProjectAsync(ProjectInfo project);

        /// <summary>
        /// Получить список недавних проектов
        /// </summary>
        Task<List<RecentProject>> GetRecentProjectsAsync();

        /// <summary>
        /// Добавить проект в список недавних
        /// </summary>
        Task AddToRecentProjectsAsync(ProjectInfo project);

        /// <summary>
        /// Удалить проект из недавних
        /// </summary>
        Task RemoveFromRecentProjectsAsync(string projectPath);
    }
}