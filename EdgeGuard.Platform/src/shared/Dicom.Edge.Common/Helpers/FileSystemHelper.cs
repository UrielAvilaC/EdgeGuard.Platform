namespace Dicom.Edge.Common.Helpers
{
    /// <summary>
    /// Helper methods for file system operations.
    /// </summary>
    public static class FileSystemHelper
    {
        /// <summary>
        /// Ensures a directory exists, creating it if necessary.
        /// </summary>
        public static async Task<bool> EnsureDirectoryExistsAsync(string path)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calculates total size of a directory in bytes.
        /// </summary>
        public static async Task<long> GetDirectorySizeAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(path))
                    return 0;

                var dirInfo = new DirectoryInfo(path);
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            });
        }

        /// <summary>
        /// Gets available disk space in bytes for the drive containing the path.
        /// </summary>
        public static async Task<long> GetAvailableSpaceAsync(string path)
        {
            return await Task.Run(() =>
            {
                var drive = new DriveInfo(Path.GetPathRoot(path) ?? path);
                return drive.AvailableFreeSpace;
            });
        }

        /// <summary>
        /// Gets total disk space in bytes for the drive containing the path.
        /// </summary>
        public static async Task<long> GetTotalSpaceAsync(string path)
        {
            return await Task.Run(() =>
            {
                var drive = new DriveInfo(Path.GetPathRoot(path) ?? path);
                return drive.TotalSize;
            });
        }

        /// <summary>
        /// Safely deletes a directory and its contents.
        /// </summary>
        public static async Task<bool> DeleteDirectoryAsync(string path, bool recursive = true)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely deletes a file.
        /// </summary>
        public static async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copies a file asynchronously with progress reporting.
        /// </summary>
        public static async Task CopyFileAsync(
            string sourcePath,
            string destinationPath,
            bool overwrite = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            const int bufferSize = 81920; // 80KB buffer
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
            using var destStream = new FileStream(destinationPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, true);

            var buffer = new byte[bufferSize];
            long totalBytes = sourceStream.Length;
            long bytesCopied = 0;

            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                bytesCopied += bytesRead;
                progress?.Report((double)bytesCopied / totalBytes * 100);
            }
        }
    }
}
