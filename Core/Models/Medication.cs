using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ORM.Attributes;

namespace Core.Models
{
    [Table("medication")]
    public class Medication
    {
        [PrimaryKey(autoIncrement: true)]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [NotNull]
        public string Name { get; set; } = null!;

        [Column("description")]
        public string? Description { get; set; }
    }
}
