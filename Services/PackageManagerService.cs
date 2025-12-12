using HCIDE.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Реализация сервиса управления пакетами
    /// </summary>
    public class PackageManagerService : IPackageManagerService
    {
        public async Task<List<PackageInfo>> GetInstalledPackagesAsync(
            ProjectType type,
            string projectPath,
            string interpreterPath)
        {
            var packages = new List<PackageInfo>();

            try
            {
                var (command, args) = GetListCommand(type, interpreterPath);

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    Log.Warning("Failed to start package list process");
                    return packages;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    packages = ParsePackageList(output, type);
                    Log.Information("Found {Count} installed packages", packages.Count);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting installed packages");
            }

            return packages;
        }

        private (string command, string args) GetListCommand(ProjectType type, string interpreterPath)
        {
            return type switch
            {
                ProjectType.Python312 => (interpreterPath, "-m pip list --format=json"),
                ProjectType.JavaScript => ("npm", "list --json --depth=0"),
                ProjectType.Go121 => (interpreterPath, "list -m -json all"),
                _ => throw new ArgumentException($"Unknown project type: {type}")
            };
        }

        private List<PackageInfo> ParsePackageList(string output, ProjectType type)
        {
            var packages = new List<PackageInfo>();

            try
            {
                switch (type)
                {
                    case ProjectType.Python312:
                        // Python pip возвращает JSON массив: [{"name": "package", "version": "1.0.0"}, ...]
                        var pythonPackages = JsonSerializer.Deserialize<List<JsonElement>>(output);
                        if (pythonPackages != null)
                        {
                            foreach (var pkg in pythonPackages)
                            {
                                packages.Add(new PackageInfo
                                {
                                    Name = pkg.GetProperty("name").GetString() ?? "",
                                    Version = pkg.GetProperty("version").GetString() ?? "",
                                    IsInstalled = true
                                });
                            }
                        }
                        break;

                    case ProjectType.JavaScript:
                        // npm list возвращает сложный JSON, упрощенная обработка
                        var npmData = JsonSerializer.Deserialize<JsonElement>(output);
                        if (npmData.TryGetProperty("dependencies", out var deps))
                        {
                            foreach (var prop in deps.EnumerateObject())
                            {
                                var version = "";
                                if (prop.Value.TryGetProperty("version", out var ver))
                                {
                                    version = ver.GetString() ?? "";
                                }

                                packages.Add(new PackageInfo
                                {
                                    Name = prop.Name,
                                    Version = version,
                                    IsInstalled = true
                                });
                            }
                        }
                        break;

                    case ProjectType.Go121:
                        // Go modules - построчный JSON
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            try
                            {
                                var module = JsonSerializer.Deserialize<JsonElement>(line);
                                if (module.TryGetProperty("Path", out var path) &&
                                    module.TryGetProperty("Version", out var version))
                                {
                                    packages.Add(new PackageInfo
                                    {
                                        Name = path.GetString() ?? "",
                                        Version = version.GetString() ?? "",
                                        IsInstalled = true
                                    });
                                }
                            }
                            catch
                            {
                                // Пропускаем невалидные строки
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing package list");
            }

            return packages;
        }

        public async Task<bool> InstallPackageAsync(
            ProjectType type,
            string packageName,
            string projectPath,
            string interpreterPath,
            IProgress<string> progress)
        {
            try
            {
                progress?.Report($"Installing {packageName}...");

                var (command, args) = GetInstallCommand(type, interpreterPath, packageName);

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    progress?.Report("Failed to start installation process");
                    return false;
                }

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        progress?.Report(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        progress?.Report($"[ERROR] {e.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                progress?.Report(success
                    ? $"✓ {packageName} installed successfully"
                    : $"✗ Failed to install {packageName}");

                Log.Information("Package {Package} installation result: {Success}", packageName, success);
                return success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error installing package {Package}", packageName);
                progress?.Report($"Error: {ex.Message}");
                return false;
            }
        }

        private (string command, string args) GetInstallCommand(ProjectType type, string interpreterPath, string packageName)
        {
            return type switch
            {
                ProjectType.Python312 => (interpreterPath, $"-m pip install {packageName}"),
                ProjectType.JavaScript => ("npm", $"install {packageName}"),
                ProjectType.Go121 => (interpreterPath, $"get {packageName}"),
                _ => throw new ArgumentException($"Unknown project type: {type}")
            };
        }

        public async Task<bool> UninstallPackageAsync(
            ProjectType type,
            string packageName,
            string projectPath,
            string interpreterPath)
        {
            try
            {
                var (command, args) = GetUninstallCommand(type, interpreterPath, packageName);

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                Log.Information("Package {Package} uninstallation result: {Success}", packageName, success);
                return success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error uninstalling package {Package}", packageName);
                return false;
            }
        }

        private (string command, string args) GetUninstallCommand(ProjectType type, string interpreterPath, string packageName)
        {
            return type switch
            {
                ProjectType.Python312 => (interpreterPath, $"-m pip uninstall -y {packageName}"),
                ProjectType.JavaScript => ("npm", $"uninstall {packageName}"),
                ProjectType.Go121 => (interpreterPath, $"mod edit -droprequire {packageName}"),
                _ => throw new ArgumentException($"Unknown project type: {type}")
            };
        }

        public async Task<bool> UpdatePackageAsync(
            ProjectType type,
            string packageName,
            string projectPath,
            string interpreterPath,
            IProgress<string> progress)
        {
            try
            {
                progress?.Report($"Updating {packageName}...");

                var (command, args) = GetUpdateCommand(type, interpreterPath, packageName);

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        progress?.Report(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                progress?.Report(success
                    ? $"✓ {packageName} updated successfully"
                    : $"✗ Failed to update {packageName}");

                return success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating package {Package}", packageName);
                return false;
            }
        }

        private (string command, string args) GetUpdateCommand(ProjectType type, string interpreterPath, string packageName)
        {
            return type switch
            {
                ProjectType.Python312 => (interpreterPath, $"-m pip install --upgrade {packageName}"),
                ProjectType.JavaScript => ("npm", $"update {packageName}"),
                ProjectType.Go121 => (interpreterPath, $"get -u {packageName}"),
                _ => throw new ArgumentException($"Unknown project type: {type}")
            };
        }

        public async Task<List<PackageInfo>> SearchPackagesAsync(ProjectType type, string query)
        {
            // Simplified implementation - в production использовать API регистров
            // PyPI: https://pypi.org/pypi/{package}/json
            // NPM: https://registry.npmjs.org/-/v1/search?text={query}
            // Go: https://pkg.go.dev/search?q={query}

            var results = new List<PackageInfo>();

            Log.Information("Package search not implemented yet for {Type}", type);

            return results;
        }
    }
}