using System.Data;
using DataAccessTracing.Tests.Mocks;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DataAccessTracing.Tests
{
    public class NpgsqlTest : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpwd")
            .Build();

        public async ValueTask InitializeAsync()
        {
            await _postgresContainer.StartAsync();

            using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE TestTable (Id NUMERIC PRIMARY KEY, Value VARCHAR(10) NOT NULL);";
            await command.ExecuteNonQueryAsync();
            for (int i = 1; i <= 100; i++)
            {
                command.CommandText = $"INSERT INTO TestTable (Id, Value) VALUES ({i}, 'Val{i}')";
                await command.ExecuteNonQueryAsync();
            }
        }

        public ValueTask DisposeAsync()
        {
            return _postgresContainer.DisposeAsync();
        }

        [Fact]
        public async Task DataReaderDispose()
        {
            void DoWork()
            {
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    CommandExecuting = (_) => { },
                    CommandExecuted = (_) => { },
                    DataReaderClosed = (_) => { },
                };
                var factory = new TracingProviderFactory(NpgsqlFactory.Instance, traceHandler);
                string connectionString = _postgresContainer.GetConnectionString();
                for (int i = 0; i < 500; i++)
                {
                    var connection = factory.CreateConnection()!;
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM TestTable";
                    using var dr = command.ExecuteReader(CommandBehavior.CloseConnection);
                    decimal id;
                    string? val;
                    while (dr.Read())
                    {
                        id = Convert.ToDecimal(dr["Id"]);
                        val = dr["Value"]?.ToString();
                    }
                }
            }

            Task[] tasks = new Task[50];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Task.Run(DoWork, TestContext.Current.CancellationToken);

            await Task.WhenAll(tasks);
        }

#if NET
        [Fact]
        public async Task DataReaderDisposeAsync()
        {
            async Task DoWork()
            {
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    CommandExecuting = (_) => { },
                    CommandExecuted = (_) => { },
                    DataReaderClosed = (_) => { },
                };
                var factory = new TracingProviderFactory(NpgsqlFactory.Instance, traceHandler);
                string connectionString = _postgresContainer.GetConnectionString();
                for (int i = 0; i < 500; i++)
                {
                    var connection = factory.CreateConnection()!;
                    connection.ConnectionString = connectionString;
                    await connection.OpenAsync(TestContext.Current.CancellationToken);
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM TestTable";
                    await using var dr = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, TestContext.Current.CancellationToken);
                    decimal id;
                    string? val;
                    while (await dr.ReadAsync(TestContext.Current.CancellationToken))
                    {
                        id = await dr.GetFieldValueAsync<decimal>(dr.GetOrdinal("Id"), TestContext.Current.CancellationToken);
                        val = await dr.GetFieldValueAsync<string>(dr.GetOrdinal("Value"), TestContext.Current.CancellationToken);
                    }
                }
            }

            Task[] tasks = new Task[50];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Task.Run(DoWork, TestContext.Current.CancellationToken);

            await Task.WhenAll(tasks);
        }
#endif
    }
}
