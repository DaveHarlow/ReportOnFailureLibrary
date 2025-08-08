namespace ReportOnFailure.Resolvers;

using Enums;
using Factories;
using Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

public class DbResolver : IDbResolver
{
    private readonly IResultFormatterFactory _formatterFactory;
    private readonly IDbProviderFactoryFactory _dbProviderFactoryFactory;


    public DbResolver(
        IResultFormatterFactory formatterFactory,
        IDbProviderFactoryFactory dbProviderFactoryFactory
    )
    {
        _formatterFactory = formatterFactory;
        _dbProviderFactoryFactory = dbProviderFactoryFactory;
    }

    public async Task<string> ResolveAsync(
        IDbReporter reporter,
        CancellationToken cancellationToken = default
    )
    {
        var factory = _dbProviderFactoryFactory.GetFactory(reporter.DatabaseType);

        await using var connection = factory.CreateConnection();
        if (connection == null)
            throw new InvalidOperationException($"The provider factory for {reporter.DatabaseType} could not create a connection.");

        connection.ConnectionString = reporter.ConnectionString;
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        if (command == null)
            throw new InvalidOperationException($"The connection for {reporter.DatabaseType} could not create a command.");

        command.CommandText = reporter.Query;
        command.CommandTimeout = reporter.CommandTimeout;
        command.Parameters.AddRange(reporter.Parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = await ReadDataAsync(reader, cancellationToken);

        return FormatResults(results, reporter.ResultsFormat);
    }

    public string ResolveSync(IDbReporter reporter)
    {
        var factory = _dbProviderFactoryFactory.GetFactory(reporter.DatabaseType);

        using var connection = factory.CreateConnection();
        if (connection == null)
            throw new InvalidOperationException($"The provider factory for {reporter.DatabaseType} could not create a connection.");

        connection.ConnectionString = reporter.ConnectionString;
        connection.Open();

        using var command = connection.CreateCommand();
        if (command == null)
            throw new InvalidOperationException($"The connection for {reporter.DatabaseType} could not create a command.");

        command.CommandText = reporter.Query;
        command.CommandTimeout = reporter.CommandTimeout;
        command.Parameters.AddRange(reporter.Parameters.ToArray());

        using var reader = command.ExecuteReader();
        var results = ReadData(reader);

        return FormatResults(results, reporter.ResultsFormat);
    }

    private static async Task<List<Dictionary<string, object?>>> ReadDataAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var results = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = value;
            }
            results.Add(row);
        }
        return results;
    }

    private static List<Dictionary<string, object?>> ReadData(DbDataReader reader)
    {
        var results = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = value;
            }
            results.Add(row);
        }
        return results;
    }

    private string FormatResults(
        IReadOnlyCollection<Dictionary<string, object?>> data,
        ResultsFormat format
    )
    {
        if (data == null || data.Count == 0)
        {
            return "Query executed successfully. No records were returned.";
        }
        return _formatterFactory.CreateFormatter(format).Format(data);
    }
}