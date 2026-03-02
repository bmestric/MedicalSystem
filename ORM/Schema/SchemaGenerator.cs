using ORM.Mapping;
using System.Text;

namespace ORM.Schema
{
    public class SchemaGenerator
    {
        public string GenerateCreateTable(EntityMetadata metadata)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE IF NOT EXISTS {metadata.TableName} (");

            var columnDefinitions = new List<string>();
            var constraints = new List<string>();

            foreach (var prop in metadata.Properties.Where(p => !p.IsNavigationProperty))
            {
                var colDef = new StringBuilder();
                colDef.Append($"    {prop.ColumnName} {prop.DatabaseType}");

                if (prop.IsPrimaryKey && prop.AutoIncrement)
                {
                    colDef.Clear();
                    colDef.Append($"    {prop.ColumnName} SERIAL");
                }

                if (prop.IsPrimaryKey)
                {
                    colDef.Append(" PRIMARY KEY");
                }

                if (prop.IsNotNull && !prop.IsPrimaryKey)
                {
                    colDef.Append(" NOT NULL");
                }

                if (prop.IsUnique)
                {
                    colDef.Append(" UNIQUE");
                }

                if (prop.DefaultValue != null)
                {
                    colDef.Append($" DEFAULT {FormatDefaultValue(prop.DefaultValue)}");
                }

                columnDefinitions.Add(colDef.ToString());

                if (prop.IsForeignKey && prop.ReferencedTable != null)
                {
                    var referencedColumn = prop.ReferencedColumn ?? "id";
                    var fkConstraint = $"    CONSTRAINT fk_{metadata.TableName}_{prop.ColumnName} " +
                                       $"FOREIGN KEY ({prop.ColumnName}) REFERENCES {prop.ReferencedTable}({referencedColumn})";

                    if (prop.OnDelete != null)
                        fkConstraint += $" ON DELETE {prop.OnDelete}";
                    if (prop.OnUpdate != null)
                        fkConstraint += $" ON UPDATE {prop.OnUpdate}";

                    constraints.Add(fkConstraint);
                }
            }

            sb.AppendLine(string.Join(",\n", columnDefinitions.Concat(constraints)));
            sb.Append(");");

            return sb.ToString();
        }

        public string GenerateSchema(params Type[] entityTypes)
        {
            var metadataList = entityTypes
                .Select(EntityMapper.GetMetadata)
                .ToList();

            var ordered = TopologicalSort(metadataList);

            var sb = new StringBuilder();
            foreach (var metadata in ordered)
            {
                sb.AppendLine(GenerateCreateTable(metadata));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static List<EntityMetadata> TopologicalSort(List<EntityMetadata> metadataList)
        {
            var sorted = new List<EntityMetadata>();
            var visited = new HashSet<string>();
            var lookup = metadataList.ToDictionary(m => m.TableName, m => m);

            void Visit(EntityMetadata metadata)
            {
                if (visited.Contains(metadata.TableName))
                    return;

                // Visit dependencies first
                foreach (var fk in metadata.ForeignKeys)
                {
                    if (fk.ReferencedTable != null && lookup.TryGetValue(fk.ReferencedTable, out var dependency))
                    {
                        Visit(dependency);
                    }
                }

                visited.Add(metadata.TableName);
                sorted.Add(metadata);
            }

            foreach (var metadata in metadataList)
            {
                Visit(metadata);
            }

            return sorted;
        }

        private static string FormatDefaultValue(object value)
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
