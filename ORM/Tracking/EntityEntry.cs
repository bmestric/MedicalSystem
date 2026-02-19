using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Tracking
{
    public class EntityEntry
    {
        public object Entity { get; set; } = null!;
        public EntityState State { get; set; }
        public Dictionary<string, object?>? OriginalValues { get; set; }
    }
}
