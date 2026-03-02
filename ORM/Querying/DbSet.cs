using ORM.Mapping;
using ORM.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ORM.Querying
{
    public class DbSet<T> : IEnumerable<T> where T : class, new()
    {
        private readonly DatabaseContext _context;
        private readonly EntityMetadata _metadata;

        private Expression<Func<T, bool>>? _whereExpression;
        private readonly List<(string Column, bool Ascending)> _orderByClauses = new();
        private int? _limit;
        private readonly List<string> _includes = new();

        public DbSet(DatabaseContext context)
        {
            _context = context;
            _metadata = EntityMapper.GetMetadata(typeof(T));
        }

        public void Add(T entity)
        {
            _context.ChangeTracker.Track(entity, EntityState.Added);
        }

        public void Remove(T entity)
        {
            _context.ChangeTracker.Track(entity, EntityState.Deleted);
        }

        public void Update(T entity)
        {
            _context.ChangeTracker.Track(entity, EntityState.Modified);
        }

        public DbSet<T> Where(Expression<Func<T, bool>> predicate)
        {
            var clone = CloneQuery();
            clone._whereExpression = predicate;
            return clone;
        }

        public DbSet<T> OrderBy(string columnName, bool ascending = true)
        {
            var clone = CloneQuery();
            var prop = _metadata.Properties.FirstOrDefault(p => p.PropertyName == columnName);
            var resolvedColumn = prop?.ColumnName ?? columnName;
            clone._orderByClauses.Add((resolvedColumn, ascending));
            return clone;
        }

        public DbSet<T> OrderByDescending(string columnName)
        {
            return OrderBy(columnName, ascending: false);
        }

        public DbSet<T> Take(int count)
        {
            var clone = CloneQuery();
            clone._limit = count;
            return clone;
        }

        public DbSet<T> Include(string navigationProperty)
        {
            var clone = CloneQuery();
            clone._includes.Add(navigationProperty);
            return clone;
        }

        public T? FindById(object id)
        {
            var pkColumn = _metadata.PrimaryKey?.ColumnName
                ?? throw new InvalidOperationException($"Entity {typeof(T).Name} has no primary key.");

            var sql = _context.QueryBuilder.BuildSelectById(_metadata);
            var parameters = new Dictionary<string, object>
            {
                { $"@{pkColumn}", id }
            };

            var results = _context.QueryExecutor.ExecuteQuery<T>(sql, parameters);
            var entity = results.FirstOrDefault();

            if (entity != null)
            {
                _context.ChangeTracker.Track(entity, EntityState.Unchanged);
                LoadNavigationProperties(entity);
            }

            return entity;
        }

        public List<T> ToList()
        {
            var (sql, parameters) = BuildQuery();

            var results = _context.QueryExecutor.ExecuteQuery<T>(sql, parameters);

            foreach (var entity in results)
            {
                _context.ChangeTracker.Track(entity, EntityState.Unchanged);
                LoadNavigationProperties(entity);
            }

            return results;
        }

        public T? FirstOrDefault()
        {
            return Take(1).ToList().FirstOrDefault();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private (string Sql, Dictionary<string, object>? Parameters) BuildQuery()
        {
            string sql;
            Dictionary<string, object>? parameters = null;

            if (_whereExpression != null)
            {
                var parser = new ExpressionParser(_metadata);
                var (whereClause, parsedParams) = parser.Parse(_whereExpression);
                sql = _context.QueryBuilder.BuildSelectWhere(_metadata, whereClause,
                    _orderByClauses.Count > 0 ? _orderByClauses : null, _limit);
                parameters = parsedParams;
            }
            else
            {
                sql = _context.QueryBuilder.BuildSelectAll(_metadata);
                sql += BuildOrderByClause();
                sql += BuildLimitClause();
            }

            return (sql, parameters);
        }

        private string BuildOrderByClause()
        {
            if (_orderByClauses.Count == 0)
                return string.Empty;

            var clauses = _orderByClauses.Select(o => $"{o.Column} {(o.Ascending ? "ASC" : "DESC")}");
            return $" ORDER BY {string.Join(", ", clauses)}";
        }

        private string BuildLimitClause()
        {
            return _limit.HasValue ? $" LIMIT {_limit.Value}" : string.Empty;
        }

        private void LoadNavigationProperties(T entity)
        {
            if (_includes.Count == 0)
                return;

            foreach (var include in _includes)
            {
                var navProp = _metadata.NavigationProperties
                    .FirstOrDefault(p => p.PropertyName == include);

                if (navProp == null)
                    continue;

                var foreignKeyProp = _metadata.ForeignKeys
                    .FirstOrDefault(fk => fk.PropertyName == navProp.ForeignKeyProperty);

                if (foreignKeyProp == null)
                    continue;

                var fkValue = entity.GetType().GetProperty(navProp.ForeignKeyProperty!)?.GetValue(entity);
                if (fkValue == null)
                    continue;

                var relatedType = navProp.PropertyType;
                var relatedMetadata = EntityMapper.GetMetadata(relatedType);
                var relatedPkColumn = relatedMetadata.PrimaryKey?.ColumnName ?? "id";

                var columns = string.Join(", ",
                    relatedMetadata.Properties
                        .Where(p => !p.IsNavigationProperty)
                        .Select(p => p.ColumnName));

                var sql = $"SELECT {columns} FROM {relatedMetadata.TableName} WHERE {relatedPkColumn} = @fk";
                var parameters = new Dictionary<string, object> { { "@fk", fkValue } };

                var method = typeof(QueryExecutor)
                    .GetMethod(nameof(QueryExecutor.ExecuteQuery))!
                    .MakeGenericMethod(relatedType);

                var relatedResults = method.Invoke(_context.QueryExecutor, new object?[] { sql, parameters });

                if (relatedResults is IList list && list.Count > 0)
                {
                    entity.GetType().GetProperty(include)?.SetValue(entity, list[0]);
                }
            }
        }

        private DbSet<T> CloneQuery()
        {
            var clone = new DbSet<T>(_context);
            clone._whereExpression = _whereExpression;
            clone._orderByClauses.AddRange(_orderByClauses);
            clone._limit = _limit;
            clone._includes.AddRange(_includes);
            return clone;
        }
    }
}


