using HCIDE.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Реализация сервиса для работы с проектами
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly string _appDataPath;
        private readonly string _recentProjectsPath;
        private const int MaxRecentProjects = 10;

        public ProjectService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HCIDE"
            );
            Directory.CreateDirectory(_appDataPath);
            _recentProjectsPath = Path.Combine(_appDataPath, "recent.json");
        }

        public async Task<ProjectInfo> CreateProjectAsync(string name, string path, ProjectType type)
        {
            try
            {
                // Создаем директорию проекта
                Directory.CreateDirectory(path);

                // Создаем служебную папку .youride
                var yourIdePath = Path.Combine(path, ".youride");
                Directory.CreateDirectory(yourIdePath);

                // Создаем объект проекта
                var project = new ProjectInfo
                {
                    Name = name,
                    Path = path,
                    Type = type,
                    LastOpened = DateTime.Now
                };

                // Создаем файлы в зависимости от типа проекта
                await CreateProjectFilesAsync(project);

                // Сохраняем проект
                await SaveProjectAsync(project);

                // Добавляем в недавние
                await AddToRecentProjectsAsync(project);

                Log.Information("Project created: {Name} at {Path}", name, path);
                return project;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating project {Name}", name);
                throw;
            }
        }

        private async Task CreateProjectFilesAsync(ProjectInfo project)
        {
            switch (project.Type)
            {
                case ProjectType.Python312:
                    // main.py
                    var pythonCode = @"# YourIDE Python Project
# Author: Your Name
# Created: " + DateTime.Now.ToString("yyyy-MM-dd") + @"

def main():
    print('Hello, World!')
    print('Welcome to YourIDE!')

if __name__ == '__main__':
    main()
";
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "main.py"),
                        pythonCode
                    );

                    // requirements.txt
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "requirements.txt"),
                        "# Python dependencies\n# Example: requests==2.31.0\n"
                    );

                    // README.md
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "README.md"),
                        $"# {project.Name}\n\nPython 3.12 project created with YourIDE.\n"
                    );
                    break;

                case ProjectType.JavaScript:
                    // index.js
                    var jsCode = @"// YourIDE JavaScript Project
// Author: Your Name
// Created: " + DateTime.Now.ToString("yyyy-MM-dd") + @"

function main() {
    console.log('Hello, World!');
    console.log('Welcome to YourIDE!');
}

main();
";
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "index.js"),
                        jsCode
                    );

                    // package.json
                    var packageJson = new
                    {
                        name = project.Name.ToLower().Replace(" ", "-"),
                        version = "1.0.0",
                        description = $"{project.Name} - Created with YourIDE",
                        main = "index.js",
                        type = "module",
                        scripts = new
                        {
                            start = "node index.js"
                        },
                        keywords = new string[] { },
                        author = "Your Name",
                        license = "ISC"
                    };

                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "package.json"),
                        JsonSerializer.Serialize(packageJson, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        })
                    );

                    // README.md
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "README.md"),
                        $"# {project.Name}\n\nJavaScript (Node.js) project created with YourIDE.\n"
                    );
                    break;

                case ProjectType.Go121:
                    // main.go
                    var goCode = @"// YourIDE Go Project
// Author: Your Name
// Created: " + DateTime.Now.ToString("yyyy-MM-dd") + @"

package main

import ""fmt""

func main() {
    fmt.Println(""Hello, World!"")
    fmt.Println(""Welcome to YourIDE!"")
}
";
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "main.go"),
                        goCode
                    );

                    // go.mod
                    var moduleName = project.Name.ToLower().Replace(" ", "");
                    var goMod = $"module {moduleName}\n\ngo 1.21\n";
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "go.mod"),
                        goMod
                    );

                    // README.md
                    await File.WriteAllTextAsync(
                        Path.Combine(project.Path, "README.md"),
                        $"# {project.Name}\n\nGo 1.21 project created with YourIDE.\n"
                    );
                    break;
            }
        }

        public async Task<ProjectInfo> LoadProjectAsync(string path)
        {
            try
            {
                var projectFilePath = Path.Combine(path, ".youride", "project.json");

                if (!File.Exists(projectFilePath))
                {
                    throw new FileNotFoundException(
                        $"Project file not found at: {projectFilePath}",
                        projectFilePath
                    );
                }

                var json = await File.ReadAllTextAsync(projectFilePath);
                var project = JsonSerializer.Deserialize<ProjectInfo>(json);

                if (project == null)
                {
                    throw new InvalidOperationException("Failed to deserialize project file");
                }

                // Обновляем дату последнего открытия
                project.LastOpened = DateTime.Now;
                await SaveProjectAsync(project);
                await AddToRecentProjectsAsync(project);

                Log.Information("Project loaded: {Name} from {Path}", project.Name, path);
                return project;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading project from {Path}", path);
                throw;
            }
        }

        public async Task SaveProjectAsync(ProjectInfo project)
        {
            try
            {
                var yourIdePath = Path.Combine(project.Path, ".youride");
                Directory.CreateDirectory(yourIdePath);

                var projectFilePath = Path.Combine(yourIdePath, "project.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(project, options);
                await File.WriteAllTextAsync(projectFilePath, json);

                Log.Information("Project saved: {Name}", project.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving project {Name}", project.Name);
                throw;
            }
        }

        public async Task<List<RecentProject>> GetRecentProjectsAsync()
        {
            try
            {
                if (!File.Exists(_recentProjectsPath))
                {
                    return new List<RecentProject>();
                }

                var json = await File.ReadAllTextAsync(_recentProjectsPath);
                var projects = JsonSerializer.Deserialize<List<RecentProject>>(json);

                if (projects == null)
                {
                    return new List<RecentProject>();
                }

                // Фильтруем несуществующие проекты
                projects = projects
                    .Where(p => Directory.Exists(p.Path))
                    .OrderByDescending(p => p.LastOpened)
                    .Take(MaxRecentProjects)
                    .ToList();

                return projects;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading recent projects");
                return new List<RecentProject>();
            }
        }

        public async Task AddToRecentProjectsAsync(ProjectInfo project)
        {
            try
            {
                var recentProjects = await GetRecentProjectsAsync();

                // Удаляем дубликаты
                recentProjects.RemoveAll(p =>
                    string.Equals(p.Path, project.Path, StringComparison.OrdinalIgnoreCase)
                );

                // Добавляем в начало
                recentProjects.Insert(0, new RecentProject
                {
                    Name = project.Name,
                    Path = project.Path,
                    Type = project.Type,
                    LastOpened = project.LastOpened,
                    Description = $"{project.Type} project"
                });

                // Оставляем только последние N проектов
                recentProjects = recentProjects.Take(MaxRecentProjects).ToList();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(recentProjects, options);
                await File.WriteAllTextAsync(_recentProjectsPath, json);

                Log.Information("Project added to recent list: {Name}", project.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding project to recent list");
            }
        }

        public async Task RemoveFromRecentProjectsAsync(string projectPath)
        {
            try
            {
                var recentProjects = await GetRecentProjectsAsync();
                recentProjects.RemoveAll(p =>
                    string.Equals(p.Path, projectPath, StringComparison.OrdinalIgnoreCase)
                );

                var json = JsonSerializer.Serialize(recentProjects, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_recentProjectsPath, json);
                Log.Information("Project removed from recent list: {Path}", projectPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing project from recent list");
            }
        }
    }
}