This project is to demonstrate how to use a temporary table to achive large volume batch loading in efcore. 

With the temp table, it's easier and faster to load items by ids.

When the ids are less than the threshold (currently set to 20), it uses the normal .Where(x=> ids.Contains(x.Id)) to load.
When the ids are above the threshold, it uses the temp table + join to load.


```c#
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

```
