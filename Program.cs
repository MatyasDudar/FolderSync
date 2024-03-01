using System.Security.Cryptography;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: FolderSync.exe <sourcePath> <replicaPath> <syncIntervalInSeconds> <logFilePath>");
            return;
        }

        string sourcePath = args[0];
        string replicaPath = args[1];
        if (!int.TryParse(args[2], out int syncInterval))
        {
            Console.WriteLine("Invalid synchronization interval. Please provide a valid number [int].");
            return;
        }
        string logDest = args[3];

        while (true)
        {
            try
            {
                SyncFolder(sourcePath, replicaPath, logDest);
            }
            catch (Exception e)
            {
                Log($"An error occured during synchronization: {e.Message}", LogMessageLevel.ERROR, logDest);
            }
            Thread.Sleep(syncInterval * 1000);
        }
    }

    static void SyncFolder(string sourcePath, string replicaPath, string logFilePath)
    {
        // replica directory availability
        if (!Directory.Exists(replicaPath))
        {
            Directory.CreateDirectory(replicaPath);
            Log($"Directory created: {replicaPath}", LogMessageLevel.INFO, logFilePath);
        }

        // Directory check-creation
        foreach (var sourceDirectory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var targetDirecory = sourceDirectory.Replace(sourcePath, replicaPath);
            if (!Directory.Exists(targetDirecory))
            {
                Directory.CreateDirectory(targetDirecory);
                Log($"Directory created: {targetDirecory}", LogMessageLevel.INFO, logFilePath);
            }
        }

        // File check-creation
        foreach (var sourceFile in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var replicaFile = sourceFile.Replace(sourcePath, replicaPath);
            if (!File.Exists(replicaFile))
            {
                File.Copy(sourceFile, replicaFile, true);
                Log($"File created: {replicaFile}", LogMessageLevel.INFO, logFilePath);
            }
            if (File.Exists(replicaFile) && MD5Checksum(sourceFile) != MD5Checksum(replicaFile))
            {
                File.Copy(sourceFile, replicaFile, true);
                Log($"File changed: {replicaFile}", LogMessageLevel.INFO, logFilePath);
            }
        }

        // Deleting unnecessary files 
        foreach (string replicaFile in Directory.GetFiles(replicaPath, "*", SearchOption.AllDirectories))
        {
            string sourceFile = replicaFile.Replace(replicaPath, sourcePath);
            if (!File.Exists(sourceFile))
            {
                File.Delete(replicaFile);
                Log($"File deleted: {replicaFile}", LogMessageLevel.INFO, logFilePath);
            }
        }

        // Deleting unnecessary directories 
        foreach (string replicaDirectory in Directory.GetDirectories(replicaPath, "*", SearchOption.AllDirectories))
        {
            string sourceDirectory = replicaDirectory.Replace(replicaPath, sourcePath);
            if (!Directory.Exists(sourceDirectory))
            {
                Directory.Delete(replicaDirectory);
                Log($"Directory deleted: {replicaDirectory}", LogMessageLevel.INFO, logFilePath);
            }
        }
    }

    static string MD5Checksum(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    static void Log(string message, LogMessageLevel level, string filePath)
    {
        Console.WriteLine($"{level} {message}");

        var directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath) && directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
        };

        File.AppendAllText(filePath, $"{DateTime.Now}: {level} {message}\n");
    }

    enum LogMessageLevel
    {
        INFO,
        ERROR
    }
}