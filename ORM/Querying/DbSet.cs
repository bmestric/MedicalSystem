using Microsoft.CodeAnalysis;
using ORM.Mapping;
using ORM.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Querying
{
    public class DbSet<T> : IEnumerable<T> where T : class, new()
    {
        private readonly DatabaseContext _context;
        private readonly EntityMetadata _metadata;
        private readonly List<T> _localCache = new();

        public DbSet(DatabaseContext context)
        {
            _context = context;
            _metadata = EntityMapper.GetMetadata(typeof(T));
        }
        public void Add(T entity)
        {
            _context.ChangeTracker.Track(entity, EntityState.Added);
            _localCache.Add(entity);
        }

        public void Remove(T entity)
        {
            _context.ChangeTracker.Track(entity, EntityState.Deleted);
            _localCache.Remove(entity);
        }

        public void Update(T entity)
        {
            _context.ChangeTracker.Track(entity, EntityState.Modified);
        }

        public List<T> ToList()
        {

            var sql = _context.QueryBuilder.BuildSelectAll(_metadata);

            var results = _context.QueryExecutor.ExecuteQuery<T>(sql);

            foreach (var entity in results)
            {
                _context.ChangeTracker.Track(entity, EntityState.Unchanged);
            }

            return results;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
