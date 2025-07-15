namespace ReportOnFailure.Entities;

public class DbConnection
{
    public string ConnectionString { get; set; }
    public string TableName { get; set; }
    public string SchemaName { get; set; }
    public string DatabaseName { get; set; }
    public int CommandTimeout { get; set; }

    public DbConnection(string connectionString, string tableName, string schemaName, string databaseName, int commandTimeout, string whereClause)
    {
        ConnectionString = connectionString;
        TableName = tableName;
        SchemaName = schemaName;
        DatabaseName = databaseName;
        CommandTimeout = commandTimeout;
    }
}