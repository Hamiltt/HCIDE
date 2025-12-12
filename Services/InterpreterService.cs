using HCIDE.Models;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Реализация сервиса для работы с интерпретаторами
    /// </summary>
    public class InterpreterService : IInterpreterService
    {
        private readonly HttpClient _httpClient;

        public InterpreterService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(30) // Для больших загрузок
            };
        }

        public async Task<string?> FindInterpreterAsync(ProjectType type)
        {
            try
            {
                var (command, args) = type switch
                {
                    ProjectType.Python312 => ("python", "--version"),
                    ProjectType.JavaScript => ("node", "--version"),
                    ProjectType.Go121 => ("go", "version"),
                    _ => throw new ArgumentException($"Unknown project type: {type}")
                };

                // Пробуем запустить команду
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    Log.Warning("Failed to start process for {Type}", type);
                    return null;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Находим полный путь
                    var fullPath = await FindExecutablePathAsync(command);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        Log.Information("Interpreter found: {Type} at {Path}", type, fullPath);
                        return fullPath;
                    }
                }

                // Проверяем стандартные пути установки
                var standardPath = GetStandardInstallPath(type);
                if (!string.IsNullOrEmpty(standardPath) && File.Exists(standardPath))
                {
                    Log.Information("Interpreter found at standard path: {Path}", standardPath);
                    return standardPath;
                }

                Log.Warning("Interpreter not found for {Type}", type);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error finding interpreter for {Type}", type);
                return null;
            }
        }

        private async Task<string?> FindExecutablePathAsync(string command)
        {
            try
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var findCommand = isWindows ? "where" : "which";

                var psi = new ProcessStartInfo
                {
                    FileName = findCommand,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    // Берем первую строку (первый найденный путь)
                    return output.Split('\n')[0].Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error finding executable path for {Command}", command);
            }

            return null;
        }

        private string? GetStandardInstallPath(ProjectType type)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null; // Пока поддерживаем только Windows
            }

            return type switch
            {
                ProjectType.Python312 => CheckPaths(
                    @"C:\Python312\python.exe",
                    @"C:\Python311\python.exe",
                    @"C:\Python310\python.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        @"Programs\Python\Python312\python.exe")
                ),
                ProjectType.JavaScript => CheckPaths(
                    @"C:\Program Files\nodejs\node.exe",
                    @"C:\Program Files (x86)\nodejs\node.exe"
                ),
                ProjectType.Go121 => CheckPaths(
                    @"C:\Go\bin\go.exe",
                    @"C:\Program Files\Go\bin\go.exe"
                ),
                _ => null
            };
        }

        private string? CheckPaths(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }

        public async Task<bool> DownloadAndInstallAsync(
            ProjectType type,
            string targetPath,
            IProgress<double> progress,
            IProgress<string> statusProgress)
        {
            try
            {
                statusProgress?.Report($"Preparing to download {type}...");

                // Получаем URL для скачивания
                var downloadUrl = GetDownloadUrl(type);
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    Log.Error("No download URL for {Type}", type);
                    return false;
                }

                Directory.CreateDirectory(targetPath);
                var zipPath = Path.Combine(targetPath, "download.zip");

                // Скачиваем архив
                statusProgress?.Report("Downloading...");
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var percentage = (double)totalRead / totalBytes * 100;
                            progress?.Report(percentage);
                        }
                    }
                }

                // Распаковываем
                statusProgress?.Report("Extracting...");
                progress?.Report(0);

                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, targetPath, overwriteFiles: true);
                });

                // Удаляем архив
                File.Delete(zipPath);

                progress?.Report(100);
                statusProgress?.Report("Installation complete!");

                Log.Information("Successfully installed {Type} to {Path}", type, targetPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading/installing {Type}", type);
                statusProgress?.Report($"Error: {ex.Message}");
                return false;
            }
        }

        private string? GetDownloadUrl(ProjectType type)
        {
            // Актуальные URL для embeddable/portable версий
            return type switch
            {
                ProjectType.Python312 => "https://www.python.org/ftp/python/3.12.0/python-3.12.0-embed-amd64.zip",
                ProjectType.JavaScript => "https://nodejs.org/dist/v20.10.0/node-v20.10.0-win-x64.zip",
                ProjectType.Go121 => "https://go.dev/dl/go1.21.5.windows-amd64.zip",
                _ => null
            };
        }

        public async Task<string?> GetVersionAsync(ProjectType type, string interpreterPath)
        {
            try
            {
                if (!File.Exists(interpreterPath))
                {
                    return null;
                }

                var args = type switch
                {
                    ProjectType.Python312 => "--version",
                    ProjectType.JavaScript => "--version",
                    ProjectType.Go121 => "version",
                    _ => "--version"
                };

                var psi = new ProcessStartInfo
                {
                    FileName = interpreterPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var version = !string.IsNullOrWhiteSpace(output) ? output : error;
                return version.Trim();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting version for {Path}", interpreterPath);
                return null;
            }
        }

        public async Task<bool> ValidateInterpreterAsync(string interpreterPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(interpreterPath) || !File.Exists(interpreterPath))
                {
                    return false;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = interpreterPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}