using Maviray.Blazor.Components.Pro.Grid.EntityFramework.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data.Common;

namespace Maviray.Blazor.Components.Pro.Grid.EntityFramework.Tests.Fixtures;

/// <summary>An in-memory SQLite database that lives as long as this object and records every SQL command it runs.</summary>
public sealed class SqliteTestDatabase : IGridDbContextFactory, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CommandRecorder _recorder = new();

    public SqliteTestDatabase()
    {
        _connection.Open();
        Options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).AddInterceptors(_recorder).Options;

        using var context = new TestDbContext(Options);
        context.Database.EnsureCreated();
    }

    public DbContextOptions<TestDbContext> Options { get; }

    public SqliteConnection Connection => _connection;

    /// <summary>The interceptor that records commands, for contexts created outside this class.</summary>
    public IInterceptor Recorder => _recorder;

    /// <summary>The SQL text and parameter values of every command run since the last <see cref="ClearCommands"/>.</summary>
    public IReadOnlyList<RecordedCommand> Commands => _recorder.Commands;

    public void ClearCommands() => _recorder.Commands.Clear();

    public void Seed(Action<TestDbContext> seed)
    {
        using var context = new TestDbContext(Options);
        seed(context);
        context.SaveChanges();
        ClearCommands();
    }

    public EntityFrameworkGridDataSource<T> CreateSource<T>() where T : class =>
        new(this, NullLogger<EntityFrameworkGridDataSource<T>>.Instance);

    public Task<DbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<DbContext>(new TestDbContext(Options));
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    public sealed record RecordedCommand(string Sql, IReadOnlyList<object?> ParameterValues)
    {
        public string OrderByClause => Sql.Contains("ORDER BY", StringComparison.Ordinal) ? Sql[Sql.IndexOf("ORDER BY", StringComparison.Ordinal)..] : "";
    }

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<RecordedCommand> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command) =>
            Commands.Add(new RecordedCommand(command.CommandText, command.Parameters.Cast<DbParameter>().Select(p => p.Value).ToArray()));
    }
}
