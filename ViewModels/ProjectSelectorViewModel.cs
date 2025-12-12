using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCIDE;
using HCIDE.Models;
using HCIDE.Services;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace HCIDE.ViewModels
{
    /// <summary>
    /// ViewModel для окна выбора проекта
    /// </summary>
    public partial class ProjectSelectorViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly IInterpreterService _interpreterService;

        [ObservableProperty]
        private string projectName = string.Empty;

        [ObservableProperty]
        private string projectPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        [ObservableProperty]
        private int selectedProjectTypeIndex = 0;

        [ObservableProperty]
        private ObservableCollection<RecentProject> recentProjects = new();

        [ObservableProperty]
        private RecentProject? selectedRecentProject;

        [ObservableProperty]
        private double downloadProgress;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isCreatingProject;

        public ProjectSelectorViewModel(
            IProjectService projectService,
            IInterpreterService interpreterService)
        {
            _projectService = projectService;
            _interpreterService = interpreterService;

            // Загружаем недавние проекты
            LoadRecentProjectsAsync();
        }

        private async void LoadRecentProjectsAsync()
        {
            try
            {
                var recent = await _projectService.GetRecentProjectsAsync();
                RecentProjects = new ObservableCollection<RecentProject>(recent);
                Log.Information("Loaded {Count} recent projects", recent.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading recent projects");
            }
        }

        [RelayCommand]
        private void BrowsePath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select project location",
                SelectedPath = ProjectPath,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProjectPath = dialog.SelectedPath;
                Log.Debug("Project path selected: {Path}", ProjectPath);
            }
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                System.Windows.MessageBox.Show(
                    "Please enter a project name.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjectPath))
            {
                System.Windows.MessageBox.Show(
                    "Please select a project location.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            IsCreatingProject = true;
            StatusMessage = "Creating project...";

            try
            {
                var projectType = (ProjectType)SelectedProjectTypeIndex;
                var fullPath = Path.Combine(ProjectPath, ProjectName);

                // Проверяем, существует ли уже папка
                if (Directory.Exists(fullPath))
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Folder '{ProjectName}' already exists. Do you want to use it?",
                        "Folder Exists",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result != MessageBoxResult.Yes)
                    {
                        IsCreatingProject = false;
                        StatusMessage = string.Empty;
                        return;
                    }
                }

                // Проверяем интерпретатор
                StatusMessage = $"Checking for {projectType} interpreter...";
                IsDownloading = true;

                var interpreterPath = await _interpreterService.FindInterpreterAsync(projectType);

                if (string.IsNullOrEmpty(interpreterPath))
                {
                    var result = System.Windows.MessageBox.Show(
                        $"{projectType} interpreter not found on your system.\n\n" +
                        $"Would you like to download and install a portable version?\n" +
                        $"(This will be installed in your project folder)",
                        "Interpreter Not Found",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        var toolsPath = Path.Combine(fullPath, "tools", projectType.ToString().ToLower());

                        var progress = new Progress<double>(p => DownloadProgress = p);
                        var statusProgress = new Progress<string>(s => StatusMessage = s);

                        StatusMessage = $"Downloading {projectType}... (this may take a few minutes)";

                        var success = await _interpreterService.DownloadAndInstallAsync(
                            projectType,
                            toolsPath,
                            progress,
                            statusProgress
                        );

                        if (!success)
                        {
                            System.Windows.MessageBox.Show(
                                "Failed to download interpreter. Please install it manually.",
                                "Download Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            IsDownloading = false;
                            IsCreatingProject = false;
                            StatusMessage = string.Empty;
                            return;
                        }

                        // Определяем путь к исполняемому файлу
                        interpreterPath = GetInstalledInterpreterPath(projectType, toolsPath);
                    }
                    else
                    {
                        IsDownloading = false;
                        IsCreatingProject = false;
                        StatusMessage = string.Empty;
                        return;
                    }
                }

                // Создаем проект
                StatusMessage = "Creating project files...";
                var project = await _projectService.CreateProjectAsync(ProjectName, fullPath, projectType);
                project.InterpreterPath = interpreterPath;
                await _projectService.SaveProjectAsync(project);

                IsDownloading = false;
                StatusMessage = "Project created successfully!";

                Log.Information("Project created: {Name} ({Type})", ProjectName, projectType);

                // Небольшая задержка для показа сообщения
                await Task.Delay(500);

                // Открываем главное окно
                OpenMainWindow(project);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating project");
                System.Windows.MessageBox.Show(
                    $"Error creating project: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                IsDownloading = false;
                IsCreatingProject = false;
                StatusMessage = string.Empty;
            }
        }

        private string GetInstalledInterpreterPath(ProjectType type, string toolsPath)
        {
            return type switch
            {
                ProjectType.Python312 => Path.Combine(toolsPath, "python.exe"),
                ProjectType.JavaScript => Path.Combine(toolsPath, "node.exe"),
                ProjectType.Go121 => Path.Combine(toolsPath, "bin", "go.exe"),
                _ => string.Empty
            };
        }

        [RelayCommand]
        private async Task OpenRecentProject()
        {
            if (SelectedRecentProject == null)
            {
                System.Windows.MessageBox.Show(
                    "Please select a project from the list.",
                    "No Project Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                StatusMessage = "Opening project...";
                var project = await _projectService.LoadProjectAsync(SelectedRecentProject.Path);
                OpenMainWindow(project);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening recent project");
                System.Windows.MessageBox.Show(
                    $"Error opening project: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private async Task OpenExisting()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "HCIDE Project|project.json",
                Title = "Open Project"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var projectPath = Path.GetDirectoryName(dialog.FileName);
                    if (projectPath != null)
                    {
                        projectPath = Path.GetDirectoryName(projectPath); // Выходим из .HCIDE
                        if (projectPath != null)
                        {
                            StatusMessage = "Opening project...";
                            var project = await _projectService.LoadProjectAsync(projectPath);
                            OpenMainWindow(project);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error opening existing project");
                    System.Windows.MessageBox.Show(
                        $"Error opening project: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    StatusMessage = string.Empty;
                }
            }
        }

        private void OpenMainWindow(ProjectInfo project)
        {
            try
            {
                var mainWindow = new Views.MainWindow();

                // Получаем ViewModel из DI контейнера
                var viewModel = (System.Windows.Application.Current as App)?.Services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;

                if (viewModel != null)
                {
                    viewModel.LoadProject(project);
                    mainWindow.DataContext = viewModel;
                    mainWindow.Show();

                    // Закрываем окно селектора
                    System.Windows.Application.Current.Windows
                        .OfType<Views.ProjectSelectorWindow>()
                        .FirstOrDefault()
                        ?.Close();
                }
                else
                {
                    Log.Error("Failed to get MainWindowViewModel from DI container");
                    System.Windows.MessageBox.Show("Failed to initialize main window", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening main window");
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}