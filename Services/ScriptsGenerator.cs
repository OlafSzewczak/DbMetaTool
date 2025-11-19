using DbMetaTool.Models;
using System.Text;

namespace DbMetaTool.Services
{
    public class ScriptsGenerator(string outputDirectory, DatabaseMetadata databaseMetadata)
    {
        private readonly string _outputDirectory = outputDirectory;
        private readonly DatabaseMetadata _databaseMetadata = databaseMetadata;

        public void GenerateSqlScripts()
        {
            if (_databaseMetadata.DomainsMetadata.Count > 0)
                GenerateDomainScripts();

            if (_databaseMetadata.TablesMetadata.Count > 0)
                GenerateTableScripts();

            if (_databaseMetadata.ProceduresMetadata.Count > 0)
                GenerateProcedureScripts();
        }

        private void GenerateDomainScripts()
        {
            var sb = new StringBuilder();
            foreach (var domain in _databaseMetadata.DomainsMetadata)
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
                File.WriteAllText(Path.Combine(_outputDirectory, $"create_{domain.Name}_domain.sql"), sb.ToString(), Encoding.UTF8);
                sb.Clear();
            }
        }

        private void GenerateTableScripts()
        {
            var sb = new StringBuilder();
            foreach (var tableMetadata in _databaseMetadata.TablesMetadata)
            {
                sb.AppendLine($"CREATE TABLE {tableMetadata.Name} (");

                var fieldLines = new List<string>();
                foreach (var fieldMetadata in tableMetadata.FieldsMetadata)
                {
                    var line = $"  {fieldMetadata.Name} {fieldMetadata.DataType}";

                    if (!string.IsNullOrEmpty(fieldMetadata.DefaultValue))
                        line += $" {fieldMetadata.DefaultValue}";

                    if (fieldMetadata.NotNull)
                        line += " NOT NULL";

                    fieldLines.Add(line);
                }

                sb.AppendLine(string.Join(",\n", fieldLines));
                sb.AppendLine(");");

                File.WriteAllText(Path.Combine(_outputDirectory, $"create_{tableMetadata.Name}_table.sql"), sb.ToString(), Encoding.UTF8);

                sb.Clear();
            }
        }

        private void GenerateProcedureScripts()
        {
            var sb = new StringBuilder();

            foreach (var procedure in _databaseMetadata.ProceduresMetadata)
            {
                sb.AppendLine($"CREATE PROCEDURE {procedure.Name}");

                // Dodaj parametry wejściowe
                var inputParams = procedure.Parameters.Where(p => p.ParameterType == 0).OrderBy(p => p.Position).ToList();
                if (inputParams.Any())
                {
                    sb.AppendLine("(");
                    for (int i = 0; i < inputParams.Count; i++)
                    {
                        var param = inputParams[i];
                        sb.Append($"    {param.Name} {param.DataType}");
                        if (i < inputParams.Count - 1)
                            sb.AppendLine(",");
                        else
                            sb.AppendLine();
                    }
                    sb.AppendLine(")");
                }

                // Dodaj parametry wyjściowe (jeśli są)
                var outputParams = procedure.Parameters.Where(p => p.ParameterType == 1).OrderBy(p => p.Position).ToList();
                if (outputParams.Any())
                {
                    sb.AppendLine("RETURNS");
                    sb.AppendLine("(");
                    for (int i = 0; i < outputParams.Count; i++)
                    {
                        var param = outputParams[i];
                        sb.Append($"    {param.Name} {param.DataType}");
                        if (i < outputParams.Count - 1)
                            sb.AppendLine(",");
                        else
                            sb.AppendLine();
                    }
                    sb.AppendLine(")");
                }

                // Dodaj AS przed ciałem procedury
                sb.AppendLine("AS");
                sb.AppendLine(procedure.Source);
                sb.Append(';');

                File.WriteAllText(
                    Path.Combine(outputDirectory, $"create_{procedure.Name}_procedure.sql"),
                    sb.ToString(),
                    Encoding.UTF8
                );

                sb.Clear();
            }
        }
    }
}
