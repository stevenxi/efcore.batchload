using efcore.batchload.data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace efcore.batchload.data
{
    public abstract class DataContextBase : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TempIdTable>().HasKey(x=> new { x.PrivateId, x.RefId });
        }

        private int _tempTableIndex = 0;

        protected DataContextBase(DbContextOptions options) : base(options)
        {
        }

        public TempIdTableContainer CreateTempIdTable() => new TempIdTableContainer(this, new Lazy<int>(() => _tempTableIndex++));


        public async Task<ICollection<TEntity>> LoadByIds<TEntity>(IQueryable<TEntity> query, IEnumerable<int> ids, Expression<Func<TEntity, int?>> keySelector)
        {
            using var tempTable = CreateTempIdTable();

            tempTable.AddIds(ids);

            return await tempTable.Filter(query, keySelector).ToListAsync();
        }

    }
}
