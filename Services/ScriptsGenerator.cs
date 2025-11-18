using DbMetaTool.Models;
using System.Text;

namespace DbMetaTool.Services
{
    public class ScriptsGenerator
    {
        public static void GenerateSqlScripts(DatabaseMetadata metadata, string outputDirectory)
        {
            // Generuj domeny
            if (metadata.Domains.Count > 0)
                GenerateDomainScripts(metadata, outputDirectory);

            // Generuj tabele
            //if (metadata.Tables.Count > 0)
            //{
            //    var sb = new StringBuilder();
            //    sb.AppendLine("-- Tabele");
            //    sb.AppendLine();

            //    foreach (var table in metadata.Tables)
            //    {
            //        sb.AppendLine($"CREATE TABLE {table.Name} (");

            //        var fieldLines = new List<string>();
            //        foreach (var field in table.Fields)
            //        {
            //            var line = $"  {field.Name} {field.DataType}";

            //            if (!string.IsNullOrEmpty(field.DefaultValue))
            //                line += $" {field.DefaultValue}";

            //            if (field.NotNull)
            //                line += " NOT NULL";

            //            fieldLines.Add(line);
            //        }

            //        sb.AppendLine(string.Join(",\n", fieldLines));
            //        sb.AppendLine(");");
            //        sb.AppendLine();
            //    }

            //    File.WriteAllText(Path.Combine(outputDirectory, "02_tables.sql"), sb.ToString(), Encoding.UTF8);
            //    Console.WriteLine($"  ✓ 02_tables.sql ({metadata.Tables.Count} tabel)");
            //}

            //// Generuj procedury
            //if (metadata.Procedures.Count > 0)
            //{
            //    var sb = new StringBuilder();
            //    sb.AppendLine("-- Procedury");
            //    sb.AppendLine();

            //    foreach (var proc in metadata.Procedures)
            //    {
            //        sb.AppendLine($"CREATE PROCEDURE {proc.Name}");
            //        sb.AppendLine(proc.Source);
            //        sb.AppendLine(";");
            //        sb.AppendLine();
            //    }

            //    File.WriteAllText(Path.Combine(outputDirectory, "03_procedures.sql"), sb.ToString(), Encoding.UTF8);
            //    Console.WriteLine($"  ✓ 03_procedures.sql ({metadata.Procedures.Count} procedur)");
            //}
        }

        private static void GenerateDomainScripts(DatabaseMetadata metadata, string outputDirectory)
        {
            var sb = new StringBuilder();
            foreach (var domain in metadata.Domains)
            {
                sb.AppendLine($"CREATE DOMAIN {domain.Name} AS {domain.DataType}");

                if (!string.IsNullOrEmpty(domain.DefaultValue))
                    sb.AppendLine($"  {domain.DefaultValue}");

                if (!string.IsNullOrEmpty(domain.CheckConstraint))
                    sb.AppendLine($"  {domain.CheckConstraint}");

                if (domain.NotNull)
                    sb.AppendLine("  NOT NULL");

                sb.Append(';');

                //TODO error handling
                File.WriteAllText(Path.Combine(outputDirectory, $"create_{domain.Name}_domain.sql"), sb.ToString(), Encoding.UTF8);
                sb.Clear();
            }
        }

        //public void GenerateScripts(string outputDirectory, string format = "sql")
        //{

        //    // 2) Pobierz metadane domen, tabel (z kolumnami) i procedur
        //    var metadata = new DatabaseMetadata
        //    {
        //        Domains = ExtractDomains(connection)
        //        //Tables = ExtractTables(connection),
        //        //Procedures = ExtractProcedures(connection)
        //    };

        //    Console.WriteLine($"Znaleziono: " +
        //        $"\n{metadata.Domains.Count} domen");

        //    // 3) Wygeneruj pliki .sql / .json / .txt w outputDirectory
        //    if (!Directory.Exists(outputDirectory))
        //        Directory.CreateDirectory(outputDirectory);

        //    switch (format.ToLower())
        //    {
        //        case "sql":
        //            GenerateSqlScripts(metadata, outputDirectory);
        //            break;
        //        //case "json":
        //        //    GenerateJsonScripts(metadata, outputDirectory);
        //        //    break;
        //        //case "txt":
        //        //    GenerateTxtScripts(metadata, outputDirectory);
        //        //    break;
        //        default:
        //            throw new ArgumentException($"Nieobsługiwany format: {format}");
        //    }

        //    Console.WriteLine($"\n✓ Skrypty zapisane w katalogu: {outputDirectory}");
        //}
    }
}
