using System;
using System.Collections.Generic;

namespace ORM.Tracking
{
    public class EntityEntry
    {
        public object Entity { get; set; } = null!;
        public EntityState State { get; set; }
        public Dictionary<string, object?>? OriginalValues { get; set; }
    }
}
