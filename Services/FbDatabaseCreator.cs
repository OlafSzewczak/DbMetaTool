using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services
{
    public static class FbDatabaseCreator
    {
        const string FbDbFileExtension = ".fdb";

        public static void Create(string databaseDirectory, string databaseName)
        {
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);

            var dbPath = Path.Combine(databaseDirectory, databaseName + FbDbFileExtension);

            var connectionString = new FbConnectionStringBuilder
            {
                Database = dbPath,
                UserID = "SYSDBA",
                Password = "masterkey",
                ServerType = FbServerType.Default // jeśli lokalny serwer, użyj Default lub Embedded
            };

            try
            {
                FbConnection.CreateDatabase(connectionString.ToString());
            }
            catch (FbException ex)
            {
                Console.WriteLine("Błąd przy tworzeniu bazy danych: " + ex.Message);
                throw;
            }
        }
    }
}
