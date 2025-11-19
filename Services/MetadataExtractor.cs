using DbMetaTool.Models;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services
{
    public class MetadataExtractor(FbConnection connection)
    {
        private readonly FbConnection _connection = connection;

        public DatabaseMetadata? ExtractMetadata()
        {
            try
            {
                _connection.Open();
                return new DatabaseMetadata
                {
                    DomainsMetadata = ExtractDomainsMetadata(),
                    TablesMetadata = ExtractTablesMetadata(),
                    ProceduresMetadata = ExtractProceduresMetadata()
            };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open database connection.");
                Console.WriteLine(ex);
                return null;
            }
        }

        private List<DomainMetadata> ExtractDomainsMetadata()
        {
            var domains = new List<DomainMetadata>();

            var query = @"
                SELECT 
                    f.RDB$FIELD_NAME as DOMAIN_NAME,
                    f.RDB$FIELD_TYPE as FIELD_TYPE,
                    f.RDB$FIELD_LENGTH as FIELD_LENGTH,
                    f.RDB$FIELD_PRECISION as FIELD_PRECISION,
                    f.RDB$FIELD_SCALE as FIELD_SCALE,
                    f.RDB$NULL_FLAG as NULL_FLAG,
                    f.RDB$DEFAULT_SOURCE as DEFAULT_SOURCE,
                    f.RDB$VALIDATION_SOURCE as CHECK_CONSTRAINT
                FROM RDB$FIELDS f
                WHERE f.RDB$FIELD_NAME NOT STARTING WITH 'RDB$'
                AND f.RDB$SYSTEM_FLAG = 0
                ORDER BY f.RDB$FIELD_NAME";

            using var cmd = new FbCommand(query, _connection);
            using var reader = cmd.ExecuteReader();

            try
            {
                while (reader.Read())
                {
                    var fieldType = Convert.ToInt32(reader["FIELD_TYPE"]);
                    var fieldLength = reader["FIELD_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_LENGTH"]) : 0;
                    var fieldPrecision = reader["FIELD_PRECISION"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_PRECISION"]) : 0;
                    var fieldScale = reader["FIELD_SCALE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_SCALE"]) : 0;

                    var domain = new DomainMetadata
                    {
                        Name = reader["DOMAIN_NAME"].ToString().Trim(),
                        DataType = GetDataTypeString(fieldType, fieldLength, fieldPrecision, fieldScale),
                        NotNull = reader["NULL_FLAG"] != DBNull.Value && Convert.ToInt32(reader["NULL_FLAG"]) == 1,
                        DefaultValue = reader["DEFAULT_SOURCE"] != DBNull.Value ? reader["DEFAULT_SOURCE"].ToString().Trim() : null,
                        CheckConstraint = reader["CHECK_CONSTRAINT"] != DBNull.Value ? reader["CHECK_CONSTRAINT"].ToString().Trim() : null
                    };

                    domains.Add(domain);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to extract domains: " + e);
            }

            Console.WriteLine($"Extracted {domains.Count} domains");
            return domains;
        }

        private List<TableMetadata> ExtractTablesMetadata()
        {
            var tables = new List<TableMetadata>();

            var tableNames = GetTableNames();
            if (tableNames.Count == 0)
            {
                Console.WriteLine("Nie znaleziono tabel w bazie danych");
                return [];
            }

            foreach (var tableName in tableNames)
            {
                var table = new TableMetadata { Name = tableName };

                var fieldQuery = @"
                            SELECT 
                                rf.RDB$FIELD_NAME as FIELD_NAME,
                                rf.RDB$FIELD_SOURCE as FIELD_SOURCE,
                                rf.RDB$NULL_FLAG as NULL_FLAG,
                                rf.RDB$DEFAULT_SOURCE as DEFAULT_SOURCE,
                                f.RDB$FIELD_TYPE as FIELD_TYPE,
                                f.RDB$FIELD_LENGTH as FIELD_LENGTH,
                                f.RDB$FIELD_PRECISION as FIELD_PRECISION,
                                f.RDB$FIELD_SCALE as FIELD_SCALE,
                                rf.RDB$FIELD_POSITION as FIELD_POSITION
                            FROM RDB$RELATION_FIELDS rf
                            JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                            WHERE rf.RDB$RELATION_NAME = @TableName
                            ORDER BY rf.RDB$FIELD_POSITION";

                using var fieldCmd = new FbCommand(fieldQuery, connection);
                fieldCmd.Parameters.AddWithValue("@TableName", tableName);
                using var fieldReader = fieldCmd.ExecuteReader();

                while (fieldReader.Read())
                {
                    var fieldSource = fieldReader["FIELD_SOURCE"].ToString().Trim();

                    var field = new TableField
                    {
                        Name = fieldReader["FIELD_NAME"].ToString().Trim(),
                        NotNull = fieldReader["NULL_FLAG"] != DBNull.Value && Convert.ToInt32(fieldReader["NULL_FLAG"]) == 1,
                        DefaultValue = fieldReader["DEFAULT_SOURCE"] != DBNull.Value ? fieldReader["DEFAULT_SOURCE"].ToString().Trim() : null,
                        Position = Convert.ToInt32(fieldReader["FIELD_POSITION"])
                    };

                    // Sprawdź czy pole używa domeny czy typu podstawowego
                    if (IsFieldSourceBaseType(fieldSource))
                    {
                        var fieldType = Convert.ToInt32(fieldReader["FIELD_TYPE"]);
                        var fieldLength = fieldReader["FIELD_LENGTH"] != DBNull.Value ? Convert.ToInt32(fieldReader["FIELD_LENGTH"]) : 0;
                        var fieldPrecision = fieldReader["FIELD_PRECISION"] != DBNull.Value ? Convert.ToInt32(fieldReader["FIELD_PRECISION"]) : 0;
                        var fieldScale = fieldReader["FIELD_SCALE"] != DBNull.Value ? Convert.ToInt32(fieldReader["FIELD_SCALE"]) : 0;

                        field.DataType = GetDataTypeString(fieldType, fieldLength, fieldPrecision, fieldScale);
                    }
                    else
                    {
                        field.DataType = fieldSource;
                    }

                    table.FieldsMetadata.Add(field);
                }

                tables.Add(table);
            }

            return tables;
        }

        private static bool IsFieldSourceBaseType(string? fieldSource)
        {
            return fieldSource.StartsWith("RDB$");
        }

        private List<ProcedureMetadata> ExtractProceduresMetadata()
        {
            var procedures = new List<ProcedureMetadata>();

            var query = @"
                SELECT 
                    RDB$PROCEDURE_NAME,
                    RDB$PROCEDURE_SOURCE
                FROM RDB$PROCEDURES
                WHERE RDB$SYSTEM_FLAG = 0
                ORDER BY RDB$PROCEDURE_NAME";

            using var cmd = new FbCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var procedure = new ProcedureMetadata
                {
                    Name = reader["RDB$PROCEDURE_NAME"].ToString().Trim(),
                    Source = reader["RDB$PROCEDURE_SOURCE"] != DBNull.Value
                        ? reader["RDB$PROCEDURE_SOURCE"].ToString()
                        : ""
                };

                // Pobierz parametry procedury
                procedure.Parameters = ExtractProcedureParameters(procedure.Name);

                procedures.Add(procedure);
            }

            Console.WriteLine($"  - Pobrano {procedures.Count} procedur");
            return procedures;
        }

        private List<ProcedureParameterMetadata> ExtractProcedureParameters(string procedureName)
        {
            var parameters = new List<ProcedureParameterMetadata>();

            var query = @"
                    SELECT 
                        pp.RDB$PARAMETER_NAME as PARAM_NAME,
                        pp.RDB$PARAMETER_TYPE as PARAM_TYPE,
                        pp.RDB$PARAMETER_NUMBER as PARAM_NUMBER,
                        f.RDB$FIELD_TYPE as FIELD_TYPE,
                        f.RDB$FIELD_LENGTH as FIELD_LENGTH,
                        f.RDB$FIELD_PRECISION as FIELD_PRECISION,
                        f.RDB$FIELD_SCALE as FIELD_SCALE,
                        pp.RDB$FIELD_SOURCE as FIELD_SOURCE
                    FROM RDB$PROCEDURE_PARAMETERS pp
                    JOIN RDB$FIELDS f ON pp.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                    WHERE pp.RDB$PROCEDURE_NAME = @ProcedureName
                    ORDER BY pp.RDB$PARAMETER_TYPE, pp.RDB$PARAMETER_NUMBER";

            using var cmd = new FbCommand(query, connection);
            cmd.Parameters.AddWithValue("@ProcedureName", procedureName);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var fieldSource = reader["FIELD_SOURCE"].ToString().Trim();
                string dataType;

                // Sprawdź czy parametr używa domeny czy typu podstawowego
                if (fieldSource.StartsWith("RDB$"))
                {
                    var fieldType = Convert.ToInt32(reader["FIELD_TYPE"]);
                    var fieldLength = reader["FIELD_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_LENGTH"]) : 0;
                    var fieldPrecision = reader["FIELD_PRECISION"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_PRECISION"]) : 0;
                    var fieldScale = reader["FIELD_SCALE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_SCALE"]) : 0;

                    dataType = GetDataTypeString(fieldType, fieldLength, fieldPrecision, fieldScale);
                }
                else
                {
                    // Używa domeny
                    dataType = fieldSource;
                }

                var parameter = new ProcedureParameterMetadata
                {
                    Name = reader["PARAM_NAME"].ToString().Trim(),
                    DataType = dataType,
                    ParameterType = Convert.ToInt32(reader["PARAM_TYPE"]),
                    Position = Convert.ToInt32(reader["PARAM_NUMBER"])
                };

                parameters.Add(parameter);
            }

            return parameters;
        }

        private List<string> GetTableNames()
        {
            var tableQuery = @"
                SELECT RDB$RELATION_NAME
                FROM RDB$RELATIONS
                WHERE RDB$SYSTEM_FLAG = 0 
                AND RDB$VIEW_BLR IS NULL
                ORDER BY RDB$RELATION_NAME";

            using var tableCmd = new FbCommand(tableQuery, _connection);
            using var tableReader = tableCmd.ExecuteReader();

            var tableNames = new List<string>();
            while (tableReader.Read())
            {
                tableNames.Add(tableReader["RDB$RELATION_NAME"].ToString().Trim());
            }
            tableReader.Close();

            return tableNames;
        }

        private static string GetDataTypeString(int fieldType, int fieldLength, int precision, int scale)
        {
            return fieldType switch
            {
                7 => scale < 0 ? $"NUMERIC({precision},{Math.Abs(scale)})" : "SMALLINT",
                8 => scale < 0 ? $"NUMERIC({precision},{Math.Abs(scale)})" : "INTEGER",
                10 => "FLOAT",
                12 => "DATE",
                13 => "TIME",
                14 => $"CHAR({fieldLength})",
                16 => scale < 0 ? $"NUMERIC({precision},{Math.Abs(scale)})" : "BIGINT",
                27 => "DOUBLE PRECISION",
                35 => "TIMESTAMP",
                37 => $"VARCHAR({fieldLength})",
                261 => "BLOB SUB_TYPE TEXT",
                _ => $"UNKNOWN_TYPE_{fieldType}"
            };
        }
    }
}
