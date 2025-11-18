using DbMetaTool.Models;
using FirebirdSql.Data.FirebirdClient;
using System.Text;

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
                    Domains = ExtractDomains(_connection)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open database connection.");
                Console.WriteLine(ex);
                return null;
            }
        }

        private static List<Domain> ExtractDomains(FbConnection connection)
        {
            var domains = new List<Domain>();

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

            using var cmd = new FbCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            try
            {
                while (reader.Read())
                {
                    var fieldType = Convert.ToInt32(reader["FIELD_TYPE"]);
                    var fieldLength = reader["FIELD_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_LENGTH"]) : 0;
                    var fieldPrecision = reader["FIELD_PRECISION"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_PRECISION"]) : 0;
                    var fieldScale = reader["FIELD_SCALE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_SCALE"]) : 0;

                    var domain = new Domain
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
