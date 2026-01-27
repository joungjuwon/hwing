using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

public class ConsoleLogExporter : EditorWindow
{
    [MenuItem("Tools/Export Console Log to File")]
    public static void ExportConsoleLog()
    {
        string path = EditorUtility.SaveFilePanel(
            "Save Console Log",
            Application.dataPath,
            "console_log_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            "txt"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        var logs = GetConsoleLogs();
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Unity Console Log Export ===");
        sb.AppendLine($"Exported at: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Total entries: {logs.Count}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();
        
        foreach (var log in logs)
        {
            sb.AppendLine($"[{log.type}] {log.message}");
            if (!string.IsNullOrEmpty(log.stackTrace))
            {
                sb.AppendLine($"  Stack: {log.stackTrace.Replace("\n", "\n  ")}");
            }
            sb.AppendLine();
        }
        
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Console log exported to: {path}");
        EditorUtility.RevealInFinder(path);
    }
    
    private static List<LogEntry> GetConsoleLogs()
    {
        var logs = new List<LogEntry>();
        
        // Use reflection to access Unity's internal LogEntries class
        var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntriesType == null)
        {
            Debug.LogError("Could not find LogEntries type");
            return logs;
        }
        
        // Get the count of log entries
        var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
        if (getCountMethod == null)
        {
            Debug.LogError("Could not find GetCount method");
            return logs;
        }
        
        int count = (int)getCountMethod.Invoke(null, null);
        
        // Start getting entries
        var startMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
        var endMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
        var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
        
        if (startMethod == null || endMethod == null || getEntryMethod == null)
        {
            Debug.LogError("Could not find required methods");
            return logs;
        }
        
        // Get LogEntry type
        var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
        if (logEntryType == null)
        {
            Debug.LogError("Could not find LogEntry type");
            return logs;
        }
        
        startMethod.Invoke(null, null);
        
        try
        {
            for (int i = 0; i < count; i++)
            {
                var entry = System.Activator.CreateInstance(logEntryType);
                getEntryMethod.Invoke(null, new object[] { i, entry });
                
                var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                
                string message = messageField?.GetValue(entry) as string ?? "";
                int mode = modeField != null ? (int)modeField.GetValue(entry) : 0;
                
                string logType = "Log";
                if ((mode & 1) != 0) logType = "Error";
                else if ((mode & 2) != 0) logType = "Warning";
                else if ((mode & 4) != 0) logType = "Log";
                
                logs.Add(new LogEntry { message = message, type = logType, stackTrace = "" });
            }
        }
        finally
        {
            endMethod.Invoke(null, null);
        }
        
        return logs;
    }
    
    private struct LogEntry
    {
        public string message;
        public string type;
        public string stackTrace;
    }
}
