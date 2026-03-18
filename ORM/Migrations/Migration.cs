using System.Text;

namespace ORM.Migrations
{
    public class MigrationOperation
    {
        public string Description { get; set; } = null!;
        public string UpSql { get; set; } = null!;
        public string DownSql { get; set; } = null!;
    }

    public class Migration
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<MigrationOperation> Operations { get; set; } = new();

        public string GetUpSql()
        {
            var sb = new StringBuilder();
            foreach (var op in Operations)
            {
                sb.AppendLine($"-- {op.Description}");
                sb.AppendLine(op.UpSql);
            }
            return sb.ToString();
        }

        public string GetDownSql()
        {
            var sb = new StringBuilder();
            // Reverse order for rollback
            foreach (var op in Enumerable.Reverse(Operations))
            {
                sb.AppendLine($"-- Rollback: {op.Description}");
                sb.AppendLine(op.DownSql);
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Migration: {Id} — {Name}");
            sb.AppendLine($"Created: {CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Operations ({Operations.Count}):");
            foreach (var op in Operations)
            {
                sb.AppendLine($"  • {op.Description}");
            }
            return sb.ToString();
        }
    }
}
