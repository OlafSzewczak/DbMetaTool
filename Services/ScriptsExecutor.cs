using FirebirdSql.Data.FirebirdClient;
using System.Text;

namespace DbMetaTool.Services
{
    public static class ScriptsExecutor
    {
        public static void Execute(string connectionString, string scriptsDirectory)
        {
            //TODO .sql jako stała / obsługa rozszerzeń .json oraz .txt 
            var scriptPaths = Directory.GetFiles(scriptsDirectory, "*.sql");
            if (scriptPaths.Length == 0)
            {
                Console.WriteLine($"No scripts found in directory: {scriptsDirectory}");
                return;
            }
            
            using var connection = new FbConnection(connectionString);
            connection.Open();

            var domainCommands = new List<string>();
            var tableCommands = new List<string>();
            var procedureCommands = new List<string>();
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

                        // TODO verify this
                        if (commandText.StartsWith("CREATE DOMAIN", StringComparison.OrdinalIgnoreCase))
                            domainCommands.Add(commandText);

                        if (commandText.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
                            tableCommands.Add(commandText);

                        if (commandText.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
                            procedureCommands.Add(commandText);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nie udało się wykonać skryptu {scriptPath}");
                    Console.WriteLine(ex);
                }
            }

            var allCommands = new List<string>();
            allCommands.AddRange(domainCommands);
            allCommands.AddRange(tableCommands);
            allCommands.AddRange(procedureCommands);
            foreach (var command in allCommands)
            {
                using var cmd = new FbCommand(command, connection);
                cmd.ExecuteNonQuery();
            }
        }

        private static List<string> SplitSqlScript(string script)
        {
            var commands = new List<string>();
            var current = new StringBuilder();
            bool insideProcedure = false;

            script = script.Replace("\r", "");
            var lines = script.Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--"))
                    continue;

                // Detect start of procedure
                if (!insideProcedure &&
                    line.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
                {
                    insideProcedure = true;
                }

                current.AppendLine(rawLine);

                if (insideProcedure)
                {
                    // END; must end the procedure
                    if (line.Contains("END", StringComparison.OrdinalIgnoreCase))
                    {
                        insideProcedure = false;

                        var endIndex = line.IndexOf("END", StringComparison.OrdinalIgnoreCase);
                        var index = endIndex + "END".Length;
                        var substringAfterEnd = line[index..];
                        if (substringAfterEnd.EndsWith(';'))
                        {
                            commands.Add(current.ToString().Trim());
                            current.Clear();
                        }
                    }
                }
                else
                {
                    // Outside procedures: split by semicolon
                    if (line.EndsWith(';'))
                    {
                        commands.Add(current.ToString().Trim());
                        current.Clear();
                    }
                }
            }

            return commands;
        }
    }
}
