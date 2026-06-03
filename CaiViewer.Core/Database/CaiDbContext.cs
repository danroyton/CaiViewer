using Dapper;
using Microsoft.Data.Sqlite;

namespace CaiViewer.Core.Database;

public class CaiDbContext
{
    private readonly string _connectionString;

    public CaiDbContext(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public SqliteConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task EnsureCreatedAsync()
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(Schema.Ddl);
    }
}
