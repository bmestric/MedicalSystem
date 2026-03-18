using ORM.Mapping;
using ORM.Schema;

namespace ORM.Migrations
{
    public class MigrationGenerator
    {
        private readonly DatabaseSchemaReader _schemaReader;
        private readonly SchemaGenerator _schemaGenerator = new();

        public MigrationGenerator(DatabaseSchemaReader schemaReader)
        {
            _schemaReader = schemaReader;
        }

        public Migration GenerateMigration(string name, params Type[] entityTypes)
        {
            var migration = new Migration
            {
                Id = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            var metadataList = entityTypes
                .Select(EntityMapper.GetMetadata)
                .ToList();

            var existingTables = _schemaReader.GetTableNames();

            // Detect new tables
            foreach (var metadata in metadataList)
            {
                if (!existingTables.Contains(metadata.TableName))
                {
                    var createSql = _schemaGenerator.GenerateCreateTable(metadata);
                    var dropSql = $"DROP TABLE IF EXISTS {metadata.TableName} CASCADE;";

                    migration.Operations.Add(new MigrationOperation
                    {
                        Description = $"Create table '{metadata.TableName}'",
                        UpSql = createSql,
                        DownSql = dropSql
                    });
                    continue;
                }

                // Table exists — check for column differences
                var dbColumns = _schemaReader.GetColumns(metadata.TableName);
                DetectColumnChanges(metadata, dbColumns, migration);
            }

            // Detect dropped tables (tables in DB not in entity types)
            var entityTableNames = metadataList.Select(m => m.TableName).ToHashSet();
            foreach (var dbTable in existingTables)
            {
                // Skip internal tables
                if (dbTable.StartsWith("__"))
                    continue;

                if (!entityTableNames.Contains(dbTable))
                {
                    migration.Operations.Add(new MigrationOperation
                    {
                        Description = $"Drop table '{dbTable}' (no matching entity)",
                        UpSql = $"DROP TABLE IF EXISTS {dbTable} CASCADE;",
                        DownSql = $"-- Cannot auto-recreate dropped table '{dbTable}'. Manual intervention required."
                    });
                }
            }

            return migration;
        }

        private void DetectColumnChanges(EntityMetadata metadata, List<DatabaseColumnInfo> dbColumns, Migration migration)
        {
            var dbColumnNames = dbColumns.Select(c => c.ColumnName).ToHashSet();
            var entityColumns = metadata.Properties.Where(p => !p.IsNavigationProperty).ToList();
            var entityColumnNames = entityColumns.Select(p => p.ColumnName).ToHashSet();

            // New columns (in entity, not in DB)
            foreach (var prop in entityColumns)
            {
                if (dbColumnNames.Contains(prop.ColumnName))
                    continue;

                var colType = prop.IsPrimaryKey && prop.AutoIncrement ? "SERIAL" : prop.DatabaseType;
                var nullability = prop.IsNotNull ? " NOT NULL" : "";
                var unique = prop.IsUnique ? " UNIQUE" : "";
                var defaultVal = prop.DefaultValue != null ? $" DEFAULT {FormatDefault(prop.DefaultValue)}" : "";

                var addSql = $"ALTER TABLE {metadata.TableName} ADD COLUMN {prop.ColumnName} {colType}{nullability}{unique}{defaultVal};";

                if (prop.IsForeignKey && prop.ReferencedTable != null)
                {
                    var refCol = prop.ReferencedColumn ?? "id";
                    addSql += $"\nALTER TABLE {metadata.TableName} ADD CONSTRAINT fk_{metadata.TableName}_{prop.ColumnName} " +
                              $"FOREIGN KEY ({prop.ColumnName}) REFERENCES {prop.ReferencedTable}({refCol});";
                }

                migration.Operations.Add(new MigrationOperation
                {
                    Description = $"Add column '{prop.ColumnName}' to '{metadata.TableName}'",
                    UpSql = addSql,
                    DownSql = $"ALTER TABLE {metadata.TableName} DROP COLUMN IF EXISTS {prop.ColumnName};"
                });
            }

            // Removed columns (in DB, not in entity)
            foreach (var dbCol in dbColumns)
            {
                if (entityColumnNames.Contains(dbCol.ColumnName))
                    continue;

                migration.Operations.Add(new MigrationOperation
                {
                    Description = $"Drop column '{dbCol.ColumnName}' from '{metadata.TableName}'",
                    UpSql = $"ALTER TABLE {metadata.TableName} DROP COLUMN IF EXISTS {dbCol.ColumnName};",
                    DownSql = $"ALTER TABLE {metadata.TableName} ADD COLUMN {dbCol.ColumnName} {NormalizeDbType(dbCol.DataType)}{(dbCol.IsNullable ? "" : " NOT NULL")};"
                });
            }

            // Type changes on existing columns
            foreach (var prop in entityColumns)
            {
                var dbCol = dbColumns.FirstOrDefault(c => c.ColumnName == prop.ColumnName);
                if (dbCol == null)
                    continue;

                var expectedType = NormalizeExpectedType(prop);
                var actualType = NormalizeDbType(dbCol.DataType);

                if (!string.Equals(expectedType, actualType, StringComparison.OrdinalIgnoreCase))
                {
                    migration.Operations.Add(new MigrationOperation
                    {
                        Description = $"Alter column '{prop.ColumnName}' in '{metadata.TableName}': {actualType} ? {expectedType}",
                        UpSql = $"ALTER TABLE {metadata.TableName} ALTER COLUMN {prop.ColumnName} TYPE {prop.DatabaseType} USING {prop.ColumnName}::{prop.DatabaseType};",
                        DownSql = $"ALTER TABLE {metadata.TableName} ALTER COLUMN {prop.ColumnName} TYPE {dbCol.DataType} USING {prop.ColumnName}::{dbCol.DataType};"
                    });
                }
            }
        }

        private static string NormalizeExpectedType(PropertyMetadata prop)
        {
            if (prop.IsPrimaryKey && prop.AutoIncrement)
                return "INTEGER";

            var type = prop.DatabaseType.ToUpperInvariant();
            if (type.StartsWith("VARCHAR")) return "VARCHAR";
            if (type.StartsWith("CHAR(")) return "CHAR";
            if (type.StartsWith("DECIMAL")) return "DECIMAL";
            return type;
        }

        private static string NormalizeDbType(string dbType)
        {
            return TypeMapper.NormalizePostgresType(dbType);
        }

        private static string FormatDefault(object value)
        {
            return value switch
            {
                string s => $"'{s}'",
                bool b => b ? "TRUE" : "FALSE",
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}
