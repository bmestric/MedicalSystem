using Npgsql;

namespace ORM.Migrations
{
    public class DatabaseSchemaReader
    {
        private readonly NpgsqlConnection _connection;

        public DatabaseSchemaReader(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public List<string> GetTableNames()
        {
            var tables = new List<string>();
            const string sql = """
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                ORDER BY table_name;
                """;

            using var cmd = new NpgsqlCommand(sql, _connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }
            return tables;
        }

        public List<DatabaseColumnInfo> GetColumns(string tableName)
        {
            var columns = new List<DatabaseColumnInfo>();
            const string sql = """
                SELECT column_name, data_type, is_nullable, column_default, 
                       character_maximum_length, numeric_precision, numeric_scale
                FROM information_schema.columns 
                WHERE table_schema = 'public' AND table_name = @tableName
                ORDER BY ordinal_position;
                """;

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(new DatabaseColumnInfo
                {
                    TableName = tableName,
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES",
                    ColumnDefault = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CharacterMaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    NumericPrecision = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    NumericScale = reader.IsDBNull(6) ? null : reader.GetInt32(6)
                });
            }
            return columns;
        }

        public List<DatabaseForeignKeyInfo> GetForeignKeys(string tableName)
        {
            var fks = new List<DatabaseForeignKeyInfo>();
            const string sql = """
                SELECT tc.constraint_name, kcu.column_name, 
                       ccu.table_name AS referenced_table, ccu.column_name AS referenced_column
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                    ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu 
                    ON tc.constraint_name = ccu.constraint_name AND tc.table_schema = ccu.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_name = @tableName;
                """;

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fks.Add(new DatabaseForeignKeyInfo
                {
                    ConstraintName = reader.GetString(0),
                    TableName = tableName,
                    ColumnName = reader.GetString(1),
                    ReferencedTable = reader.GetString(2),
                    ReferencedColumn = reader.GetString(3)
                });
            }
            return fks;
        }

        public bool TableExists(string tableName)
        {
            const string sql = """
                SELECT COUNT(*) FROM information_schema.tables 
                WHERE table_schema = 'public' AND table_name = @tableName;
                """;

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }
    }
}
