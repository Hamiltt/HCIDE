using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCIDE;
using HCIDE.Models;
using HCIDE.Services;
using HCIDE.ViewModels;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;

namespace HCIDE.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IDisposable
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;
        private readonly IThemeService _themeService;
        private FileSystemWatcher? _fileWatcher;
        private Process? _runningProcess;
        private System.Timers.Timer? _autoSaveTimer;

        [ObservableProperty]
        private ProjectInfo? currentProject;

        [ObservableProperty]
        private ObservableCollection<FileNode> fileTree = new();

        [ObservableProperty]
        private ObservableCollection<OpenFileInfo> openFiles = new();

        [ObservableProperty]
        private OpenFileInfo? selectedFile;

        [ObservableProperty]
        private string outputText = string.Empty;

        [ObservableProperty]
        private string statusBarText = "Ready";

        [ObservableProperty]
        private int currentLine = 1;

        [ObservableProperty]
        private int currentColumn = 1;

        [ObservableProperty]
        private string currentLanguage = string.Empty;

        [ObservableProperty]
        private ThemeSettings themeSettings;

        [ObservableProperty]
        private bool isRunning;

        /// <summary>
        /// Текущий активный редактор кода (для Undo/Redo/Find)
        /// </summary>
        public TextEditor? CurrentEditor { get; set; }

        public MainWindowViewModel(
            IProjectService projectService,
            IFileService fileService,
            IThemeService themeService)
        {
            _projectService = projectService;
            _fileService = fileService;
            _themeService = themeService;
            themeSettings = _themeService.GetCurrentTheme();

            _autoSaveTimer = new System.Timers.Timer(30000);
            _autoSaveTimer.Elapsed += (s, e) => AutoSaveFiles();
            _autoSaveTimer.Start();

            Log.Information("MainWindowViewModel initialized");
        }

        public void LoadProject(ProjectInfo project)
        {
            CurrentProject = project;
            CurrentLanguage = project.Type.ToString();
            StatusBarText = $"Project: {project.Name}";

            _ = LoadThemeAsync();
            RefreshFileTree();
            WatchProjectFolder(project.Path);
            RestoreOpenFiles();

            Log.Information("Project loaded: {Name}", project.Name);
        }

        private async Task LoadThemeAsync()
        {
            if (CurrentProject == null) return;

            var theme = await _themeService.LoadThemeAsync(CurrentProject.Path);
            ThemeSettings = theme;
            _themeService.ApplyTheme(theme);
        }

        private void RefreshFileTree()
        {
            if (CurrentProject == null) return;

            FileTree.Clear();
            var rootNode = BuildFileTree(CurrentProject.Path);

            foreach (var child in rootNode.Children)
            {
                FileTree.Add(child);
            }

            Log.Debug("File tree refreshed: {Count} items", FileTree.Count);
        }

        private FileNode BuildFileTree(string path)
        {
            var node = new FileNode
            {
                Name = Path.GetFileName(path),
                FullPath = path,
                IsDirectory = Directory.Exists(path)
            };

            if (node.IsDirectory)
            {
                try
                {
                    var directories = Directory.GetDirectories(path)
                        .Where(d => !ShouldExcludeDirectory(d))
                        .OrderBy(d => Path.GetFileName(d));

                    foreach (var dir in directories)
                    {
                        node.Children.Add(BuildFileTree(dir));
                    }

                    var files = Directory.GetFiles(path)
                        .OrderBy(f => Path.GetFileName(f));

                    foreach (var file in files)
                    {
                        node.Children.Add(new FileNode
                        {
                            Name = Path.GetFileName(file),
                            FullPath = file,
                            IsDirectory = false
                        });
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Warning(ex, "Access denied to directory: {Path}", path);
                }
            }

            return node;
        }

        private bool ShouldExcludeDirectory(string path)
        {
            var dirName = Path.GetFileName(path).ToLower();
            return dirName == ".youride" || dirName == "node_modules" || dirName == "venv" ||
                   dirName == ".git" || dirName == "__pycache__" || dirName == "bin" || dirName == "obj";
        }

        [RelayCommand]
        private async Task NewFile()
        {
            if (CurrentProject == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                InitialDirectory = CurrentProject.Path,
                Filter = GetFileFilter(),
                DefaultExt = GetDefaultExtension()
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _fileService.CreateFileAsync(dialog.FileName);
                    await OpenFileAsync(dialog.FileName);
                    RefreshFileTree();
                    StatusBarText = $"Created: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error creating file");
                    System.Windows.MessageBox.Show($"Error creating file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetFileFilter()
        {
            return CurrentProject?.Type switch
            {
                ProjectType.Python312 => "Python Files|*.py|All Files|*.*",
                ProjectType.JavaScript => "JavaScript Files|*.js|JSON Files|*.json|All Files|*.*",
                ProjectType.Go121 => "Go Files|*.go|All Files|*.*",
                _ => "All Files|*.*"
            };
        }

        private string GetDefaultExtension()
        {
            return CurrentProject?.Type switch
            {
                ProjectType.Python312 => ".py",
                ProjectType.JavaScript => ".js",
                ProjectType.Go121 => ".go",
                _ => ".txt"
            };
        }

        [RelayCommand]
        private async Task OpenFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = CurrentProject?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "All Files|*.*|Python|*.py|JavaScript|*.js|Go|*.go|Text|*.txt",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    await OpenFileAsync(fileName);
                }
            }
        }

        public async Task OpenFileAsync(string filePath)
        {
            try
            {
                var existing = OpenFiles.FirstOrDefault(f =>
                    string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    SelectedFile = existing;
                    return;
                }

                var content = await _fileService.ReadFileAsync(filePath);
                var extension = Path.GetExtension(filePath).ToLower();

                var fileInfo = new OpenFileInfo
                {
                    Name = Path.GetFileName(filePath),
                    FilePath = filePath,
                    Document = new TextDocument(content),
                    SyntaxHighlighting = GetSyntaxHighlighting(extension),
                    IsDirty = false
                };

                fileInfo.Document.TextChanged += (s, e) =>
                {
                    fileInfo.IsDirty = true;
                    UpdateStatusBar();
                };

                OpenFiles.Add(fileInfo);
                SelectedFile = fileInfo;

                StatusBarText = $"Opened: {fileInfo.Name}";
                Log.Information("File opened: {Path}", filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening file: {Path}", filePath);
                System.Windows.MessageBox.Show($"Error opening file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IHighlightingDefinition? GetSyntaxHighlighting(string extension)
        {
            return extension switch
            {
                ".py" => HighlightingManager.Instance.GetDefinition("Python"),
                ".js" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".json" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".go" => HighlightingManager.Instance.GetDefinition("Go"),
                ".cs" => HighlightingManager.Instance.GetDefinition("C#"),
                ".xml" => HighlightingManager.Instance.GetDefinition("XML"),
                ".html" => HighlightingManager.Instance.GetDefinition("HTML"),
                ".css" => HighlightingManager.Instance.GetDefinition("CSS"),
                _ => null
            };
        }

        [RelayCommand]
        private async Task SaveFile()
        {
            if (SelectedFile == null) return;

            try
            {
                await _fileService.WriteFileAsync(SelectedFile.FilePath, SelectedFile.Document.Text);
                SelectedFile.IsDirty = false;
                StatusBarText = $"Saved: {SelectedFile.Name}";
                Log.Information("File saved: {Path}", SelectedFile.FilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving file");
                System.Windows.MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SaveAll()
        {
            var dirtyFiles = OpenFiles.Where(f => f.IsDirty).ToList();

            foreach (var file in dirtyFiles)
            {
                try
                {
                    await _fileService.WriteFileAsync(file.FilePath, file.Document.Text);
                    file.IsDirty = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error saving file: {Path}", file.FilePath);
                }
            }

            StatusBarText = $"Saved {dirtyFiles.Count} file(s)";
            Log.Information("Saved {Count} files", dirtyFiles.Count);
        }

        [RelayCommand]
        private void CloseFile(OpenFileInfo fileInfo)
        {
            if (fileInfo.IsDirty)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Save changes to {fileInfo.Name}?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    _fileService.WriteFileAsync(fileInfo.FilePath, fileInfo.Document.Text).Wait();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            OpenFiles.Remove(fileInfo);
            StatusBarText = $"Closed: {fileInfo.Name}";
        }

        [RelayCommand]
        private async Task RunProject()
        {
            if (CurrentProject == null || IsRunning) return;

            await SaveAll();

            IsRunning = true;
            StatusBarText = "Running...";
            OutputText = $"=== Running {CurrentProject.Name} ==={Environment.NewLine}";

            try
            {
                var (command, args, workingDir) = GetRunCommand();

                if (string.IsNullOrEmpty(command))
                {
                    OutputText += "[ERROR] No main file found. Please create main.py, index.js, or main.go" + Environment.NewLine;
                    IsRunning = false;
                    StatusBarText = "Run failed";
                    return;
                }

                _runningProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        WorkingDirectory = workingDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _runningProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            OutputText += e.Data + Environment.NewLine;
                        });
                    }
                };

                _runningProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            OutputText += $"[ERROR] {e.Data}{Environment.NewLine}";
                        });
                    }
                };

                _runningProcess.Start();
                _runningProcess.BeginOutputReadLine();
                _runningProcess.BeginErrorReadLine();

                await _runningProcess.WaitForExitAsync();

                var exitCode = _runningProcess.ExitCode;
                OutputText += Environment.NewLine + $"=== Process exited with code {exitCode} ==={Environment.NewLine}";
                StatusBarText = $"Finished (Exit code: {exitCode})";

                Log.Information("Project run completed with exit code: {ExitCode}", exitCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running project");
                OutputText += $"{Environment.NewLine}[ERROR] {ex.Message}{Environment.NewLine}";
                StatusBarText = "Run failed";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private (string command, string args, string workingDir) GetRunCommand()
        {
            if (CurrentProject == null)
                return (string.Empty, string.Empty, string.Empty);

            string mainFile;
            switch (CurrentProject.Type)
            {
                case ProjectType.Python312:
                    mainFile = Path.Combine(CurrentProject.Path, "main.py");
                    if (!File.Exists(mainFile))
                    {
                        return (string.Empty, string.Empty, string.Empty);
                    }
                    return (CurrentProject.InterpreterPath, "main.py", CurrentProject.Path);

                case ProjectType.JavaScript:
                    mainFile = Path.Combine(CurrentProject.Path, "index.js");
                    if (!File.Exists(mainFile))
                    {
                        return (string.Empty, string.Empty, string.Empty);
                    }
                    return (CurrentProject.InterpreterPath, "index.js", CurrentProject.Path);

                case ProjectType.Go121:
                    mainFile = Path.Combine(CurrentProject.Path, "main.go");
                    if (!File.Exists(mainFile))
                    {
                        return (string.Empty, string.Empty, string.Empty);
                    }
                    return (CurrentProject.InterpreterPath, "run main.go", CurrentProject.Path);

                default:
                    return (string.Empty, string.Empty, string.Empty);
            }
        }

        [RelayCommand]
        private void Stop()
        {
            if (_runningProcess != null && !_runningProcess.HasExited)
            {
                try
                {
                    _runningProcess.Kill(entireProcessTree: true);
                    OutputText += Environment.NewLine + "[INFO] Process terminated by user" + Environment.NewLine;
                    StatusBarText = "Stopped";
                    IsRunning = false;
                    Log.Information("Process terminated by user");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error stopping process");
                }
            }
        }

        [RelayCommand]
        private void OpenThemeSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            var viewModel = new SettingsViewModel(_themeService, CurrentProject?.Path);

            viewModel.ThemeApplied += (s, theme) =>
            {
                ThemeSettings = theme;
            };

            settingsWindow.DataContext = viewModel;
            settingsWindow.ShowDialog();
        }

        [RelayCommand]
        private void Find()
        {
            if (CurrentEditor != null)
            {
                var searchPanel = ICSharpCode.AvalonEdit.Search.SearchPanel.Install(CurrentEditor);
                searchPanel.Open();
                searchPanel.SearchPattern = string.Empty; // Очищаем предыдущий поиск
            }
            else
            {
                System.Windows.MessageBox.Show("Please open a file first", "Find",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void Undo()
        {
            if (CurrentEditor != null && CurrentEditor.CanUndo)
            {
                CurrentEditor.Undo();
                StatusBarText = "Undo";
            }
        }

        [RelayCommand]
        private void Redo()
        {
            if (CurrentEditor != null && CurrentEditor.CanRedo)
            {
                CurrentEditor.Redo();
                StatusBarText = "Redo";
            }
        }

        [RelayCommand]
        private async Task CloseProject()
        {
            var dirtyFiles = OpenFiles.Where(f => f.IsDirty).ToList();

            if (dirtyFiles.Any())
            {
                var result = System.Windows.MessageBox.Show(
                    $"You have {dirtyFiles.Count} unsaved file(s). Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    await SaveAll();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            if (CurrentProject != null)
            {
                CurrentProject.OpenFiles = OpenFiles.Select(f => f.FilePath).ToList();
                CurrentProject.ActiveFile = SelectedFile?.FilePath ?? string.Empty;
                await _projectService.SaveProjectAsync(CurrentProject);
            }

            var selectorWindow = new Views.ProjectSelectorWindow();
            var viewModel = (System.Windows.Application.Current as App)?.Services
                .GetService(typeof(ProjectSelectorViewModel)) as ProjectSelectorViewModel;

            if (viewModel != null)
            {
                selectorWindow.DataContext = viewModel;
                selectorWindow.Show();
            }

            System.Windows.Application.Current.Windows
                .OfType<Views.MainWindow>()
                .FirstOrDefault()
                ?.Close();
        }

        [RelayCommand]
        private void Exit()
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void AutoSaveFiles()
        {
            if (CurrentProject == null) return;

            try
            {
                var autoSavePath = Path.Combine(CurrentProject.Path, ".youride", "autosave");
                Directory.CreateDirectory(autoSavePath);

                foreach (var file in OpenFiles.Where(f => f.IsDirty))
                {
                    try
                    {
                        var fileName = Path.GetFileName(file.FilePath);
                        var savePath = Path.Combine(autoSavePath, $"{fileName}.autosave");
                        File.WriteAllText(savePath, file.Document.Text);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Auto-save failed for {File}", file.FilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Auto-save error");
            }
        }

        private void WatchProjectFolder(string path)
        {
            try
            {
                _fileWatcher?.Dispose();

                _fileWatcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    IncludeSubdirectories = true
                };

                _fileWatcher.Created += OnFileSystemChanged;
                _fileWatcher.Deleted += OnFileSystemChanged;
                _fileWatcher.Renamed += OnFileSystemChanged;
                _fileWatcher.EnableRaisingEvents = true;

                Log.Debug("File watcher started for: {Path}", path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting up file watcher");
            }
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RefreshFileTree();
            });
        }

        private void RestoreOpenFiles()
        {
            if (CurrentProject == null) return;

            Task.Run(async () =>
            {
                foreach (var filePath in CurrentProject.OpenFiles)
                {
                    if (File.Exists(filePath))
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await OpenFileAsync(filePath);
                        });
                    }
                }

                if (!string.IsNullOrEmpty(CurrentProject.ActiveFile) &&
                    File.Exists(CurrentProject.ActiveFile))
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var activeFile = OpenFiles.FirstOrDefault(f => f.FilePath == CurrentProject.ActiveFile);
                        if (activeFile != null)
                        {
                            SelectedFile = activeFile;
                        }
                    });
                }
            });
        }

        private void UpdateStatusBar()
        {
            if (SelectedFile != null && SelectedFile.IsDirty)
            {
                StatusBarText = $"Modified: {SelectedFile.Name}";
            }
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
            _runningProcess?.Dispose();
            _autoSaveTimer?.Dispose();
        }
    }

    public partial class OpenFileInfo : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string filePath = string.Empty;

        [ObservableProperty]
        private TextDocument document = new();

        [ObservableProperty]
        private IHighlightingDefinition? syntaxHighlighting;

        [ObservableProperty]
        private bool isDirty;
    }
}