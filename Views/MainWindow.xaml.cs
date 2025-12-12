using HCIDE;
using HCIDE.Models;
using HCIDE.Services;
using HCIDE.ViewModels;
using ICSharpCode.AvalonEdit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HCIDE.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Подписываемся на событие закрытия окна
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel == null) return;

            // Проверяем несохраненные файлы
            var dirtyFiles = viewModel.OpenFiles.Where(f => f.IsDirty).ToList();

            if (dirtyFiles.Any())
            {
                var result = System.Windows.MessageBox.Show(
                    $"You have {dirtyFiles.Count} unsaved file(s). Do you want to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.SaveAllCommand.ExecuteAsync(null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Сохраняем информацию о проекте
            if (viewModel.CurrentProject != null)
            {
                viewModel.CurrentProject.OpenFiles = viewModel.OpenFiles
                    .Select(f => f.FilePath)
                    .ToList();

                viewModel.CurrentProject.ActiveFile = viewModel.SelectedFile?.FilePath ?? string.Empty;

                // Сохраняем проект (не ждем завершения, чтобы не замедлять закрытие)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var projectService = (System.Windows.Application.Current as App)?.Services
                            .GetService(typeof(IProjectService)) as IProjectService;
                        if (projectService != null)
                        {
                            await projectService.SaveProjectAsync(viewModel.CurrentProject);
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Error saving project on close");
                    }
                });
            }

            // Dispose ViewModel
            viewModel.Dispose();
        }

        private async void FileTreeItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // Double-click
            {
                var stackPanel = sender as StackPanel;
                if (stackPanel?.DataContext is FileNode fileNode && !fileNode.IsDirectory)
                {
                    var viewModel = DataContext as MainWindowViewModel;
                    if (viewModel != null)
                    {
                        await viewModel.OpenFileAsync(fileNode.FullPath);
                    }
                }
            }
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var editor = sender as TextEditor;
            var viewModel = DataContext as MainWindowViewModel;

            if (editor != null && viewModel != null)
            {
                viewModel.CurrentEditor = editor; // ← теперь работает!

                editor.TextArea.Caret.PositionChanged += (s, args) =>
                {
                    viewModel.CurrentLine = editor.TextArea.Caret.Line;
                    viewModel.CurrentColumn = editor.TextArea.Caret.Column;
                };
            }
        }
    }
}