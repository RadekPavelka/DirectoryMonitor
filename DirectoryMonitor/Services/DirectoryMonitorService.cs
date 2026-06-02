using DirectoryMonitor.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DirectoryMonitor.Services
{
    public class DirectoryMonitorService
    {
        private const string SnapshotDirectory = "Data\\snapshots";

        public async Task<AnalysisResponse> AnalyzeAsync(string path)
        {
            try
            {
                var previousSnapshot = await LoadSnapshotAsync(path);
                var currentSnapshot = await CreateSnapshotAsync(path);

                if (previousSnapshot == null)
                {
                    await SaveSnapshotAsync(path, currentSnapshot);
                    return new AnalysisResponse(new AnalysisResult(), true, null);
                }

                var result = Compare(previousSnapshot, currentSnapshot);
                await SaveSnapshotAsync(path, currentSnapshot);
                return new AnalysisResponse(result, false, null);
            }
            catch (UnauthorizedAccessException)
            {
                return new AnalysisResponse(new AnalysisResult(), false, "Přístup k adresáři nebo některým souborům byl odepřen.");
            }
            catch (IOException ex)
            {
                return new AnalysisResponse(new AnalysisResult(), false, $"Chyba při čtení souborů: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new AnalysisResponse(new AnalysisResult(), false, $"Neočekávaná chyba: {ex.Message}");
            }
        }

        private async Task<DirectorySnapshot> CreateSnapshotAsync(string rootPath)
        {
            var snapshot = new DirectorySnapshot
            {
                RootPath = rootPath
            };

            foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                snapshot.Directories.Add(Path.GetRelativePath(rootPath, dir));
            }

            foreach (var file in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                snapshot.Files.Add(new FileMetadata
                {
                    RelativePath = Path.GetRelativePath(rootPath, file),
                    Hash = await CalculateHashAsync(file),
                    Version = 1
                });
            }

            return snapshot;
        }

        private async Task<string> CalculateHashAsync(string filePath)
        {
            using var sha = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }

        private AnalysisResult Compare(DirectorySnapshot previous, DirectorySnapshot current)
        {
            var result = new AnalysisResult();
            var previousFiles = previous.Files.ToDictionary(x => x.RelativePath);
            var currentFiles = current.Files.ToDictionary(x => x.RelativePath);

            foreach (var currentFile in current.Files)
            {
                if (!previousFiles.TryGetValue(currentFile.RelativePath, out var oldFile))
                {
                    result.NewFiles.Add(currentFile.RelativePath);
                }
                else if (oldFile.Hash != currentFile.Hash)
                {
                    result.ChangedFiles.Add(currentFile.RelativePath);
                    currentFile.Version = oldFile.Version + 1;
                }
                else
                {
                    currentFile.Version = oldFile.Version;
                }

                result.FileVersions[currentFile.RelativePath] = currentFile.Version;
            }

            foreach (var oldFile in previous.Files)
            {
                if (!currentFiles.ContainsKey(oldFile.RelativePath))
                {
                    result.DeletedFiles.Add(oldFile.RelativePath);
                }
            }

            result.DeletedDirectories = previous.Directories
                .Except(current.Directories)
                .ToList();

            return result;
        }

        private async Task SaveSnapshotAsync(string rootPath, DirectorySnapshot snapshot)
        {
            Directory.CreateDirectory(SnapshotDirectory);
            var snapshotFilePath = GetSnapshotFilePath(rootPath);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(snapshotFilePath, json);
        }

        private async Task<DirectorySnapshot?> LoadSnapshotAsync(string rootPath)
        {
            var snapshotFilePath = GetSnapshotFilePath(rootPath);
            if (!File.Exists(snapshotFilePath))
                return null;

            var json = await File.ReadAllTextAsync(snapshotFilePath);
            return JsonSerializer.Deserialize<DirectorySnapshot>(json);
        }

        private static string GetSnapshotFilePath(string rootPath)
        {
            var normalizedPath = Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();

            var pathHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath)));
            return Path.Combine(SnapshotDirectory, $"{pathHash}.json");
        }
    }
}
