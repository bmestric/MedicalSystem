using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Tracking
{
    public class ChangeTracker
    {
        private readonly ConcurrentDictionary<object, EntityEntry> _trackedEntities = new();

        public void Track(object entity, EntityState state)
        {
            if (_trackedEntities.TryGetValue(entity, out var entry))
            {
                entry.State = state;
            }
            else
            {
                var newEntry = new EntityEntry
                {
                    Entity = entity,
                    State = state,
                    OriginalValues = state == EntityState.Unchanged || state == EntityState.Modified ? CreateSnapshot(entity) : null
                };
                _trackedEntities[entity] = newEntry;
            }
        }

        public EntityState GetState(object entity) => _trackedEntities.TryGetValue(entity, out var entry) ? entry.State : EntityState.Detached;

        public IEnumerable<EntityEntry> GetEntries(EntityState? state = null)
        {
            if (state.HasValue)
            {
                return _trackedEntities.Values.Where(e => e.State == state.Value);
            }
            return _trackedEntities.Values;
        }

        public void DetectChanges()
        {
            foreach (var entry in _trackedEntities.Values.Where(e => e.State == EntityState.Unchanged))
            {
                if (HasChanges(entry))
                {
                    entry.State = EntityState.Modified;
                }
            }
        }

        public void AcceptAllChanges()
        {
            foreach (var entry in _trackedEntities.Values)
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    entry.State = EntityState.Unchanged;
                    entry.OriginalValues = CreateSnapshot(entry.Entity);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    _trackedEntities.TryRemove(entry.Entity, out _);
                }
            }
        }

        private Dictionary<string, object?> CreateSnapshot(object entity)
        {
            var snapshot = new Dictionary<string, object?>();
            var properties = entity.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.CanRead)
                {
                    snapshot[prop.Name] = prop.GetValue(entity);
                }
            }

            return snapshot;
        }

        private bool HasChanges(EntityEntry entry)
        {
            if (entry.OriginalValues == null)
                return false;

            var currentValues = CreateSnapshot(entry.Entity);

            foreach (var kvp in entry.OriginalValues)
            {
                if (!currentValues.TryGetValue(kvp.Key, out var currentValue))
                    continue;

                if (!Equals(kvp.Value, currentValue))
                    return true;
            }

            return false;
        }

    }
}
