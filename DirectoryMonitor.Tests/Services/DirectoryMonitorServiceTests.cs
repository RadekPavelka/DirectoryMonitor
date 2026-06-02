using DirectoryMonitor.Models;
using DirectoryMonitor.Services;
using System.Reflection;

namespace DirectoryMonitor.Tests.Services
{
    public class DirectoryMonitorServiceTests
    {
        [Fact]
        public void Compare_NewFile_ShouldBeDetected()
        {
            // Arrange
            var service = new DirectoryMonitorService();
            var compareMethod = GetPrivateMethod("Compare");

            var previous = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>()
            };

            var current = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "new.txt", Hash = "ABC123", Version = 1 }
                }
            };

            // Act
            var result = (AnalysisResult)compareMethod.Invoke(service, new object[] { previous, current })!;

            // Assert
            Assert.Single(result.NewFiles);
            Assert.Contains("new.txt", result.NewFiles);
            Assert.Empty(result.ChangedFiles);
            Assert.Empty(result.DeletedFiles);
        }

        [Fact]
        public void Compare_ChangedFile_ShouldBeDetectedAndVersionIncremented()
        {
            // Arrange
            var service = new DirectoryMonitorService();
            var compareMethod = GetPrivateMethod("Compare");

            var previous = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "file.txt", Hash = "OLD_HASH", Version = 2 }
                }
            };

            var current = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "file.txt", Hash = "NEW_HASH", Version = 1 }
                }
            };

            // Act
            var result = (AnalysisResult)compareMethod.Invoke(service, new object[] { previous, current })!;

            // Assert
            Assert.Single(result.ChangedFiles);
            Assert.Contains("file.txt", result.ChangedFiles);
            Assert.Equal(3, result.FileVersions["file.txt"]); // Version incremented from 2 to 3
            Assert.Empty(result.NewFiles);
            Assert.Empty(result.DeletedFiles);
        }

        [Fact]
        public void Compare_UnchangedFile_ShouldKeepSameVersion()
        {
            // Arrange
            var service = new DirectoryMonitorService();
            var compareMethod = GetPrivateMethod("Compare");

            var previous = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "file.txt", Hash = "SAME_HASH", Version = 5 }
                }
            };

            var current = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "file.txt", Hash = "SAME_HASH", Version = 1 }
                }
            };

            // Act
            var result = (AnalysisResult)compareMethod.Invoke(service, new object[] { previous, current })!;

            // Assert
            Assert.Equal(5, result.FileVersions["file.txt"]); // Version preserved
            Assert.Empty(result.NewFiles);
            Assert.Empty(result.ChangedFiles);
            Assert.Empty(result.DeletedFiles);
        }

        [Fact]
        public void Compare_DeletedFile_ShouldBeDetected()
        {
            // Arrange
            var service = new DirectoryMonitorService();
            var compareMethod = GetPrivateMethod("Compare");

            var previous = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "deleted.txt", Hash = "ABC123", Version = 1 }
                }
            };

            var current = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>()
            };

            // Act
            var result = (AnalysisResult)compareMethod.Invoke(service, new object[] { previous, current })!;

            // Assert
            Assert.Single(result.DeletedFiles);
            Assert.Contains("deleted.txt", result.DeletedFiles);
            Assert.Empty(result.NewFiles);
            Assert.Empty(result.ChangedFiles);
        }

        [Fact]
        public void Compare_DeletedDirectory_ShouldBeDetected()
        {
            // Arrange
            var service = new DirectoryMonitorService();
            var compareMethod = GetPrivateMethod("Compare");

            var previous = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>(),
                Directories = new List<string> { "OldDir", "StillHere" }
            };

            var current = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>(),
                Directories = new List<string> { "StillHere" }
            };

            // Act
            var result = (AnalysisResult)compareMethod.Invoke(service, new object[] { previous, current })!;

            // Assert
            Assert.Single(result.DeletedDirectories);
            Assert.Contains("OldDir", result.DeletedDirectories);
        }

        [Fact]
        public void Compare_MixedChanges_ShouldDetectAll()
        {
            // Arrange
            var service = new DirectoryMonitorService();
            var compareMethod = GetPrivateMethod("Compare");

            var previous = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "unchanged.txt", Hash = "HASH1", Version = 1 },
                    new FileMetadata { RelativePath = "changed.txt", Hash = "OLD", Version = 2 },
                    new FileMetadata { RelativePath = "deleted.txt", Hash = "HASH3", Version = 1 }
                },
                Directories = new List<string> { "dir1", "dir2" }
            };

            var current = new DirectorySnapshot
            {
                RootPath = "C:\\Test",
                Files = new List<FileMetadata>
                {
                    new FileMetadata { RelativePath = "unchanged.txt", Hash = "HASH1", Version = 1 },
                    new FileMetadata { RelativePath = "changed.txt", Hash = "NEW", Version = 1 },
                    new FileMetadata { RelativePath = "new.txt", Hash = "HASH4", Version = 1 }
                },
                Directories = new List<string> { "dir1" }
            };

            // Act
            var result = (AnalysisResult)compareMethod.Invoke(service, new object[] { previous, current })!;

            // Assert
            Assert.Single(result.NewFiles);
            Assert.Contains("new.txt", result.NewFiles);

            Assert.Single(result.ChangedFiles);
            Assert.Contains("changed.txt", result.ChangedFiles);
            Assert.Equal(3, result.FileVersions["changed.txt"]);

            Assert.Single(result.DeletedFiles);
            Assert.Contains("deleted.txt", result.DeletedFiles);

            Assert.Single(result.DeletedDirectories);
            Assert.Contains("dir2", result.DeletedDirectories);

            Assert.Equal(1, result.FileVersions["unchanged.txt"]);
        }

        private MethodInfo GetPrivateMethod(string methodName)
        {
            var method = typeof(DirectoryMonitorService).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(method);
            return method;
        }
    }
}
