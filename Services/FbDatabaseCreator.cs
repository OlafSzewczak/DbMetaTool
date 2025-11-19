using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services
{
    public static class FbDatabaseCreator
    {
        const string FbDbFileExtension = ".fdb";

        public static string? CreateEmpty(string databaseDirectory, string databaseName)
        {
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);

            var dbPath = Path.Combine(databaseDirectory, databaseName + FbDbFileExtension);
            if (File.Exists(dbPath))
            {
                Console.WriteLine($"Database file at path: {dbPath} already exists");
                // Think if you want to delete this
                File.Delete(dbPath);
                //return null;
            }

            var connectionStringBuilder = new FbConnectionStringBuilder
            {
                DataSource = "localhost",
                Database = dbPath,
                UserID = "SYSDBA",
                Password = "firebirdolaf",
                ServerType = FbServerType.Default,
                Charset = "UTF8"
            };

            try
            {
                var connectionString = connectionStringBuilder.ToString();
                FbConnection.CreateDatabase(connectionString);
                return connectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create the database");
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
