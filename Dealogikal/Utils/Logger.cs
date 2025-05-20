using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;


namespace Dealogikal.Utils
{
	public class Logger
	{
        private static string logFilePath = @"C:\Logs\appLog.txt"; // Define your log file path here
        public static void Log(string message)
        {
            try
            {
                // Append the message to the log file with timestamp
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // If logging fails, you could handle it here
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }
    }
}