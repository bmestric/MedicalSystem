using ORM.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ORM.Querying
{
    public class QueryBuilder
    {
        public string BuildSelectAll(EntityMetadata metadata)
        {
            var columns = GetColumnList(metadata);
            return $"SELECT {columns} FROM {metadata.TableName}";
        }

        public string BuildSelectById(EntityMetadata metadata)
        {
            var columns = GetColumnList(metadata);
            var pkColumn = GetPrimaryKeyColumn(metadata);
            return $"SELECT {columns} FROM {metadata.TableName} WHERE {pkColumn} = @{pkColumn}";
        }

        public string BuildSelectWhere(EntityMetadata metadata, string whereClause,
            List<(string Column, bool Ascending)>? orderBy = null, int? limit = null)
        {
            var columns = GetColumnList(metadata);
            var sql = $"SELECT {columns} FROM {metadata.TableName} WHERE {whereClause}";

            if (orderBy != null && orderBy.Count > 0)
            {
                var orderClauses = orderBy.Select(o => $"{o.Column} {(o.Ascending ? "ASC" : "DESC")}");
                sql += $" ORDER BY {string.Join(", ", orderClauses)}";
            }

            if (limit.HasValue)
            {
                sql += $" LIMIT {limit.Value}";
            }

            return sql;
        }

        public string BuildSelectWithJoin(EntityMetadata metadata, EntityMetadata relatedMetadata,
            string foreignKeyColumn, string referencedColumn)
        {
            var mainColumns = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Select(p => $"{metadata.TableName}.{p.ColumnName}");

            var relatedColumns = relatedMetadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Select(p => $"{relatedMetadata.TableName}.{p.ColumnName} AS {relatedMetadata.TableName}_{p.ColumnName}");

            var allColumns = string.Join(", ", mainColumns.Concat(relatedColumns));

            return $"SELECT {allColumns} FROM {metadata.TableName} " +
                   $"LEFT JOIN {relatedMetadata.TableName} ON {metadata.TableName}.{foreignKeyColumn} = {relatedMetadata.TableName}.{referencedColumn}";
        }

        public string BuildInsert(EntityMetadata metadata)
        {
            var insertColumns = GetInsertColumnList(metadata);
            var insertParameters = GetInsertParameterList(metadata);
            var pkColumn = GetPrimaryKeyColumn(metadata);

            return $"INSERT INTO {metadata.TableName} ({insertColumns}) VALUES ({insertParameters}) RETURNING {pkColumn}";
        }

        public string BuildUpdate(EntityMetadata metadata)
        {
            var setClause = GetUpdateSetClause(metadata);
            var pkColumn = GetPrimaryKeyColumn(metadata);

            return $"UPDATE {metadata.TableName} SET {setClause} WHERE {pkColumn} = @{pkColumn}";
        }

        public string BuildDelete(EntityMetadata metadata)
        {
            var pkColumn = GetPrimaryKeyColumn(metadata);
            return $"DELETE FROM {metadata.TableName} WHERE {pkColumn} = @{pkColumn}";
        }

        private static string GetPrimaryKeyColumn(EntityMetadata metadata)
        {
            return metadata.PrimaryKey?.ColumnName
                ?? throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no primary key.");
        }

        private static string GetColumnList(EntityMetadata metadata)
        {
            var columns = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Select(p => p.ColumnName)
                .ToList();

            if (columns.Count == 0)
                throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no columns to select.");

            return string.Join(", ", columns);
        }

        private static string GetInsertColumnList(EntityMetadata metadata)
        {
            var columns = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Where(p => !(p.IsPrimaryKey && p.AutoIncrement))
                .Select(p => p.ColumnName)
                .ToList();

            if (columns.Count == 0)
                throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no columns to insert.");

            return string.Join(", ", columns);
        }

        private static string GetInsertParameterList(EntityMetadata metadata)
        {
            var parameters = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Where(p => !(p.IsPrimaryKey && p.AutoIncrement))
                .Select(p => $"@{p.ColumnName}");

            return string.Join(", ", parameters);
        }

        private static string GetUpdateSetClause(EntityMetadata metadata)
        {
            var setPairs = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Where(p => !p.IsPrimaryKey)
                .Select(p => $"{p.ColumnName} = @{p.ColumnName}")
                .ToList();

            if (setPairs.Count == 0)
                throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no columns to update.");

            return string.Join(", ", setPairs);
        }
    }
}
