using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ORM.Attributes;

namespace Core.Models
{
    [Table("doctor")]
    public class Doctor
    {
        [PrimaryKey(autoIncrement: true)]
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name")]
        [NotNull]
        public string FirstName { get; set; } = null!;

        [Column("last_name")]
        [NotNull]
        public string LastName { get; set; } = null!;

        [Column("specialization")]
        [NotNull]
        public string Specialization { get; set; } = null!;

        public override string ToString()
            => $"{FirstName} {LastName} ({Specialization})";
    }
}
