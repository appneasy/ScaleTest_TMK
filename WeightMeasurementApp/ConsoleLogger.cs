using System;
using System.IO;

public class ConsoleLogger
{
    private static ConsoleLogger? _instance;
    private static readonly object _lock = new();
    private readonly string logDirectory;
    private readonly string mainLogFile;
    private readonly Dictionary<string, string> logFileCache = new();
    private StreamWriter? mainWriter;
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

    public static ConsoleLogger Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new ConsoleLogger();
            }
        }
    }

    private ConsoleLogger(string folder = "Logs")
    {
        logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);
        Directory.CreateDirectory(logDirectory);
        mainLogFile = GetLogFilePath("Main");
        mainWriter = CreateWriter(mainLogFile);
    }

    public void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string fullMessage = $"[{timestamp}] {message}";

        Console.WriteLine(fullMessage);

        try
        {
            if (mainWriter != null)
            {
                //if (CheckFileTooBig(mainLogFile))
                //    RotateFile(ref mainWriter, "Main");

                mainWriter.WriteLine(fullMessage);
                mainWriter.Flush();
            }
        }
        catch { /* ป้องกัน crash จาก log fail */ }
    }

    public void LogToFile(string category, string content)
    {
        string path = GetLogFilePath(category);
        try
        {
            using var sw = new StreamWriter(path, append: true);
            if (CheckFileTooBig(path))
            {
                sw.Close();
                File.Move(path, path.Replace(".txt", $"_{DateTime.Now:HHmmss}.bak"));
            }
            else
            {
                sw.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {content}");
            }
        }
        catch { /* ล้มเหลวก็ปล่อยผ่าน */ }
    }

    private string GetLogFilePath(string category)
    {
        if (!logFileCache.ContainsKey(category))
        {
            string path = Path.Combine(logDirectory, $"{category}_Log.txt");
            logFileCache[category] = path;
        }
        return logFileCache[category];
    }

    private StreamWriter CreateWriter(string path)
    {
        return new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
    }

    private bool CheckFileTooBig(string path)
    {
        return File.Exists(path) && new FileInfo(path).Length >= MaxFileSizeBytes;
    }

    private void RotateFile(ref StreamWriter? writer, string category)
    {
        writer?.Close();
        string oldPath = GetLogFilePath(category);
        string newPath = oldPath.Replace(".txt", $"_{DateTime.Now:HHmmss}.bak");
        File.Move(oldPath, newPath);
        writer = CreateWriter(oldPath);
    }
}
