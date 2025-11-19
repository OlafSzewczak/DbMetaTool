using FirebirdSql.Data.FirebirdClient;
using System.Text;

namespace DbMetaTool.Services
{
    public static class ScriptsExecutor
    {
        public static void Execute(string connectionString, string scriptsDirectory)
        {
            // domeny -> tabele -> procedury
            //TODO .sql jako stała / obsługa rozszerzeń .json oraz .txt 
            var scriptPaths = Directory.GetFiles(scriptsDirectory, "*.sql");
            if (scriptPaths.Length == 0)
            {
                Console.WriteLine($"No scripts found in directory: {scriptsDirectory}");
                return;
            }
            
            using var connection = new FbConnection(connectionString);
            connection.Open();

            foreach (var scriptPath in scriptPaths)
            {
                try
                {
                    var scriptContent = File.ReadAllText(scriptPath, Encoding.UTF8);
                    var commands = SplitSqlScript(scriptContent);
                    foreach (var commandText in commands)
                    {
                        if (string.IsNullOrWhiteSpace(commandText))
                            continue;

                        using var cmd = new FbCommand(commandText, connection);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ❌ Błąd w {scriptPath}: {ex.Message}");
                    throw;
                }
            }
        }

        private static List<string> SplitSqlScript(string script)
        {
            var commands = new List<string>();

            // Remove comments
            var lines = script.Split('\n');
            var cleanedScript = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                // Skip comment lines
                if (trimmedLine.StartsWith("--") || string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                cleanedScript.AppendLine(line);
            }

            // Split as commands via semicolon
            var scriptText = cleanedScript.ToString();
            var parts = scriptText.Split(';');

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    commands.Add(trimmed);
                }
            }

            return commands;
        }
    }
}
