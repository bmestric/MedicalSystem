using Npgsql;
using ORM.Mapping;
using ORM.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Querying
{
    public abstract class DatabaseContext : IDisposable
    {
        private NpgsqlConnection? _connection;
        private NpgsqlTransaction? _transaction;
        private bool _disposed;

        protected string ConnectionString { get; set; } = string.Empty;

        public ChangeTracker ChangeTracker { get; } = new();
        public QueryExecutor QueryExecutor { get; private set; } = null!;
        public QueryBuilder QueryBuilder { get; private set; } = null!;

        protected abstract void OnConfiguring();

        protected DatabaseContext()
        {
            OnConfiguring();
            QueryBuilder = new QueryBuilder();
            QueryExecutor = new QueryExecutor(this);
        }

        public NpgsqlConnection GetConnection()
        {
            if(_connection == null)
            {
                if(string.IsNullOrEmpty(ConnectionString))
                {
                    throw new InvalidOperationException("Connection string is not configured. Override OnConfiguring() to set it.");
                }

                _connection = new NpgsqlConnection(ConnectionString);
                _connection.Open();
            }

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            return _connection;
        }

        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _transaction = GetConnection().BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }

            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        public void RollbackTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        public NpgsqlTransaction? CurrentTransaction => _transaction;

        public int SaveChanges()
        {
            ChangeTracker.DetectChanges();

            var changeCount = 0;
            var wasInTransaction = _transaction != null;

            try
            {
                if (!wasInTransaction)
                {
                    BeginTransaction();
                }

                // Process deletions first (to avoid FK constraint issues)
                foreach (var entry in ChangeTracker.GetEntries(EntityState.Deleted))
                {
                    var metadata = EntityMapper.GetMetadata(entry.Entity.GetType());
                    var sql = QueryBuilder.BuildDelete(metadata);

                    // Extract PK value
                    var pkProperty = metadata.PrimaryKey
                        ?? throw new InvalidOperationException($"Entity {entry.Entity.GetType().Name} has no primary key");

                    var pkValue = entry.Entity.GetType()
                        .GetProperty(pkProperty.PropertyName)
                        ?.GetValue(entry.Entity);

                    var parameters = new Dictionary<string, object>
                    {
                        { $"@{pkProperty.ColumnName}", pkValue ?? throw new InvalidOperationException("Primary key value is null") }
                    };

                    QueryExecutor.ExecuteNonQuery(sql, parameters);
                    changeCount++;
                }

                // updates
                foreach (var entry in ChangeTracker.GetEntries(EntityState.Modified))
                {
                    var metadata = EntityMapper.GetMetadata(entry.Entity.GetType());
                    var sql = QueryBuilder.BuildUpdate(metadata);

                    // Extract all property values
                    var parameters = new Dictionary<string, object>();

                    foreach (var prop in metadata.Properties.Where(p => !p.IsNavigationProperty))
                    {
                        var propInfo = entry.Entity.GetType().GetProperty(prop.PropertyName);
                        var value = propInfo?.GetValue(entry.Entity);
                        parameters[$"@{prop.ColumnName}"] = value ?? DBNull.Value;
                    }

                    QueryExecutor.ExecuteNonQuery(sql, parameters);
                    changeCount++;
                }

                foreach (var entry in ChangeTracker.GetEntries(EntityState.Added))
                {
                    var metadata = EntityMapper.GetMetadata(entry.Entity.GetType());
                    var sql = QueryBuilder.BuildInsert(metadata);

                    var parameters = new Dictionary<string, object>();

                    foreach (var prop in metadata.Properties
                        .Where(p => !p.IsNavigationProperty)
                        .Where(p => !(p.IsPrimaryKey && p.AutoIncrement)))
                    {
                        var propInfo = entry.Entity.GetType().GetProperty(prop.PropertyName);
                        var value = propInfo?.GetValue(entry.Entity);
                        parameters[$"@{prop.ColumnName}"] = value ?? DBNull.Value;
                    }

                    var generatedId = QueryExecutor.ExecuteScalar(sql, parameters);

                    if (metadata.PrimaryKey?.AutoIncrement == true && generatedId != null)
                    {
                        var pkPropInfo = entry.Entity.GetType().GetProperty(metadata.PrimaryKey.PropertyName);
                        pkPropInfo?.SetValue(entry.Entity, Convert.ChangeType(generatedId, pkPropInfo.PropertyType));
                    }

                    changeCount++;
                }

                if (!wasInTransaction)
                {
                    CommitTransaction();
                }

                ChangeTracker.AcceptAllChanges();
            }
            catch
            {
                if (!wasInTransaction)
                {
                    RollbackTransaction();
                }
                throw;
            }

            return changeCount;
        }


        protected DbSet<T> Set<T>() where T : class, new()
        {
            return new DbSet<T>(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _connection?.Dispose();
                }

                _disposed = true;
            }
        }

    }
}
