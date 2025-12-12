using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HCIDE.Services
{
    /// <summary>
    /// Реализация сервиса для работы с файлами
    /// </summary>
    public class FileService : IFileService
    {
        public async Task<string> ReadFileAsync(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"File not found: {path}");
                }

                var content = await File.ReadAllTextAsync(path);
                Log.Debug("File read: {Path} ({Length} chars)", path, content.Length);
                return content;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading file: {Path}", path);
                throw;
            }
        }

        public async Task WriteFileAsync(string path, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(path, content);
                Log.Information("File written: {Path} ({Length} chars)", path, content.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error writing file: {Path}", path);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Log.Information("File deleted: {Path}", path);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                    Log.Information("Directory deleted: {Path}", path);
                    return true;
                }

                Log.Warning("Path not found for deletion: {Path}", path);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting: {Path}", path);
                return false;
            }
        }

        public async Task<bool> RenameAsync(string oldPath, string newPath)
        {
            try
            {
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                    Log.Information("File renamed: {Old} -> {New}", oldPath, newPath);
                    return true;
                }
                else if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                    Log.Information("Directory renamed: {Old} -> {New}", oldPath, newPath);
                    return true;
                }

                Log.Warning("Path not found for rename: {Path}", oldPath);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error renaming: {Old} -> {New}", oldPath, newPath);
                return false;
            }
        }

        public async Task<bool> CreateDirectoryAsync(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Log.Warning("Directory already exists: {Path}", path);
                    return true;
                }

                Directory.CreateDirectory(path);
                Log.Information("Directory created: {Path}", path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating directory: {Path}", path);
                return false;
            }
        }

        public async Task<bool> CreateFileAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Log.Warning("File already exists: {Path}", path);
                    return true;
                }

                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(path, string.Empty);
                Log.Information("File created: {Path}", path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating file: {Path}", path);
                return false;
            }
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"Source file not found: {sourcePath}");
                }

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourcePath, destinationPath, overwrite: true);
                Log.Information("File copied: {Source} -> {Destination}", sourcePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error copying file: {Source} -> {Destination}", sourcePath, destinationPath);
                return false;
            }
        }
    }
}