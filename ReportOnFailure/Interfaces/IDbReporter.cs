namespace ReportOnFailure.Interfaces;

public interface IDbReporter : IReporter
{
    void SetConnectionString(string connectionString);
    void SetTableName(string tableName);
    void SetSchemaName(string schemaName);
    void SetDatabaseName(string databaseName);
    void SetCommandTimeout(int timeout);
    void SetWhereClause(string whereClause);

}