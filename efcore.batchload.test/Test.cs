using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace efcore.batchload.test
{
    public class Test
    {
        [Test, Category("Local")]
        public async Task LocalTest()
        {
            var optionBuilder = new DbContextOptionsBuilder<TestDbContext>();
            
            optionBuilder.UseSqlServer("Server=localhost;Database=Playground;"
				,o => o.UseCompatibilityLevel(120) //Force efcore to use id in () instead of OpenJSON, when ids less than the threshold, where temp table not being used.  https://github.com/dotnet/efcore/issues/32394
                );

            using var context = new TestDbContext(optionBuilder.Options);

            //Load by .Contains(x.id)
            var items = await context.LoadByIds(context.Items, Enumerable.Range(1, 5), x => x.Id);


            //Load by temp table + join
            items = await context.LoadByIds(context.Items, Enumerable.Range(1, 30), x => x.Id);


            //Reuse temp table
            using (var tmpTable = context.CreateTempIdTable())
            {
                tmpTable.AddIds(Enumerable.Range(1, 100));

                items = await tmpTable.Filter(context.Items, x => x.Id).ToListAsync();

                items = await tmpTable.Filter(context.Items, x => x.Id).ToListAsync();

                items = await tmpTable.TempIdTable.Join(context.Items, x => x.RefId, y=>y.Id, (x,y) => y).ToListAsync();

            }

        }
    }
}
