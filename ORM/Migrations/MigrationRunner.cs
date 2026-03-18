using Npgsql;

namespace ORM.Migrations
{
    public class MigrationRunner
    {
        private readonly NpgsqlConnection _connection;

        public MigrationRunner(NpgsqlConnection connection)
        {
            _connection = connection;
        }
        public void EnsureMigrationTable()
        {
            const string sql = """
                CREATE TABLE IF NOT EXISTS __migrations (
                    id VARCHAR(14) PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    applied_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                    up_sql TEXT NOT NULL,
                    down_sql TEXT NOT NULL
                );
                """;

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        }

        public List<string> GetAppliedMigrations()
        {
            EnsureMigrationTable();
            var ids = new List<string>();

            const string sql = "SELECT id FROM __migrations ORDER BY id;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(reader.GetString(0));
            }
            return ids;
        }

        public void MigrateUp(Migration migration)
        {
            var applied = GetAppliedMigrations();
            if (applied.Contains(migration.Id))
            {
                Console.WriteLine($"Migration '{migration.Id}' is already applied. Skipping.");
                return;
            }

            if (migration.Operations.Count == 0)
            {
                Console.WriteLine("No changes detected. Schema is up to date.");
                return;
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                // Execute all up operations
                var upSql = migration.GetUpSql();
                using (var cmd = new NpgsqlCommand(upSql, _connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                // Record migration
                const string insertSql = """
                    INSERT INTO __migrations (id, name, applied_at, up_sql, down_sql) 
                    VALUES (@id, @name, @appliedAt, @upSql, @downSql);
                    """;
                using (var cmd = new NpgsqlCommand(insertSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@id", migration.Id);
                    cmd.Parameters.AddWithValue("@name", migration.Name);
                    cmd.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@upSql", migration.GetUpSql());
                    cmd.Parameters.AddWithValue("@downSql", migration.GetDownSql());
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Migration '{migration.Id} — {migration.Name}' applied successfully.");
                foreach (var op in migration.Operations)
                {
                    Console.WriteLine($"  ? {op.Description}");
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Migration failed: {ex.Message}");
                throw;
            }
        }

        public void MigrateDown()
        {
            var applied = GetAppliedMigrations();
            if (applied.Count == 0)
            {
                Console.WriteLine("No migrations to rollback.");
                return;
            }

            var lastId = applied.Last();

            // Read the stored down SQL
            string downSql;
            string name;
            const string selectSql = "SELECT name, down_sql FROM __migrations WHERE id = @id;";
            using (var cmd = new NpgsqlCommand(selectSql, _connection))
            {
                cmd.Parameters.AddWithValue("@id", lastId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    Console.WriteLine($"Migration '{lastId}' not found in history.");
                    return;
                }
                name = reader.GetString(0);
                downSql = reader.GetString(1);
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                using (var cmd = new NpgsqlCommand(downSql, _connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                const string deleteSql = "DELETE FROM __migrations WHERE id = @id;";
                using (var cmd = new NpgsqlCommand(deleteSql, _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@id", lastId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Rolled back migration '{lastId} — {name}'.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Rollback failed: {ex.Message}");
                throw;
            }
        }

        public void PrintStatus()
        {
            EnsureMigrationTable();

            const string sql = "SELECT id, name, applied_at FROM __migrations ORDER BY id;";
            using var cmd = new NpgsqlCommand(sql, _connection);
            using var reader = cmd.ExecuteReader();

            var hasAny = false;
            Console.WriteLine("\n--- MIGRATION HISTORY ---");
            while (reader.Read())
            {
                hasAny = true;
                Console.WriteLine($"  [{reader.GetString(0)}] {reader.GetString(1)} — applied {reader.GetDateTime(2):yyyy-MM-dd HH:mm:ss}");
            }

            if (!hasAny)
                Console.WriteLine("  No migrations applied.");
            Console.WriteLine();
        }
    }
}
