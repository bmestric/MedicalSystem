using ORM.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var pkColumn = metadata.PrimaryKey?.ColumnName
                ?? throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no primary key");

            return $"SELECT {columns} FROM {metadata.TableName} WHERE {pkColumn} = @{pkColumn}";
        }

        public string BuildInsert(EntityMetadata metadata)
        {
            var insertColumns = GetInsertColumnList(metadata);
            var insertParameters = GetInsertParameterList(metadata);
            var pkColumn = metadata.PrimaryKey?.ColumnName
               ?? throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no primary key");

            return $"INSERT INTO {metadata.TableName} ({insertColumns}) VALUES ({insertParameters}) RETURNING {pkColumn}";
        }

        public string BuildUpdate(EntityMetadata metadata)
        {
            var setClause = GetUpdateSetClause(metadata);
            var pkColumn = metadata.PrimaryKey?.ColumnName
                ?? throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no primary key");

            return $"UPDATE {metadata.TableName} SET {setClause} WHERE {pkColumn} = @{pkColumn}";
        }

        public string BuildDelete(EntityMetadata metadata)
        {
            var pkColumn = metadata.PrimaryKey?.ColumnName
                ?? throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no primary key");

            return $"DELETE FROM {metadata.TableName} WHERE {pkColumn} = @{pkColumn}";
        }



        private string GetColumnList(EntityMetadata metadata)
        {
            var columns = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Select(p => p.ColumnName);

            if (!columns.Any())
                throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no columns to select");

            return string.Join(", ", columns);
        }

        private string GetInsertColumnList(EntityMetadata metadata)
        {
            var columns = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Where(p => !(p.IsPrimaryKey && p.AutoIncrement))
                .Select(p => p.ColumnName);

            if (!columns.Any())
                throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no columns to insert");

            return string.Join(", ", columns);
        }

        private string GetInsertParameterList(EntityMetadata metadata)
        {
            var parameters = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Where(p => !(p.IsPrimaryKey && p.AutoIncrement))
                .Select(p => $"@{p.ColumnName}");

            return string.Join(", ", parameters);
        }

        private string GetUpdateSetClause(EntityMetadata metadata)
        {
            var setPairs = metadata.Properties
                .Where(p => !p.IsNavigationProperty)
                .Where(p => !p.IsPrimaryKey) // Don't update PK
                .Select(p => $"{p.ColumnName} = @{p.ColumnName}");

            if (!setPairs.Any())
                throw new InvalidOperationException($"Entity {metadata.EntityType.Name} has no columns to update");

            return string.Join(", ", setPairs);
        }
    }
}
