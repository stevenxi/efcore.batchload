using efcore.batchload.data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Principal;

namespace efcore.batchload.data
{
    public class TempIdTableContainer : OpenConnectionOwnerWrapper
    {
        private const int TempTableThreshold = 20;

        private readonly DataContextBase _context;
        public Lazy<int> TempTableIndex { get; }
        private readonly List<int> _pendingList = new List<int>();
        private readonly List<int> _insertedList = new List<int>();
        internal TempIdTableContainer(DataContextBase context, Lazy<int> tempTableIndex) 
            : base(context, true)
        {
            _context = context;
            TempTableIndex = tempTableIndex;
        }

        private bool _toDropTempTable;

        private bool _intendToCreateTable;
        private void CreateTempTable()
        {
            if (_intendToCreateTable)
                return;

            _intendToCreateTable = true;

            Open();

            const string query = @"declare @exist int
set @exist = OBJECT_ID('tempdb..#TempIdTable')
if @exist IS NULL 
BEGIN
    CREATE TABLE #TempIdTable(PrivateId int, RefId int) 
    CREATE NONCLUSTERED INDEX [_IDX_TMPIDTable] ON [dbo].[#TempIdTable] ([RefId]) INCLUDE ([PrivateId])
END
Select @exist";

            using (var cmd = OpenedConnection.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.CommandType = System.Data.CommandType.Text;
                if (_context.Database.CurrentTransaction != null)
                    cmd.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();

                _toDropTempTable = true;
                using (var result = cmd.ExecuteReader())
                {
                    while (result.Read())
                        _toDropTempTable = result.IsDBNull(0);
                }
            }
        }

        public void AddIds(IEnumerable<int> ids)
        {
            _pendingList.AddRange(ids);
        }


        private void InsertPending()
        {
            CreateTempTable();

            if (!_pendingList.Any())
                return;


            //To achieve best performance, use bulk insert here would be better.
            //But to simplify the demo, just do a standard insert here.

            _context.Set<TempIdTable>().AddRange(_pendingList.Distinct().Select(v => new TempIdTable
            {
                PrivateId = TempTableIndex.Value,
                RefId = v,
            }));
            _context.SaveChanges();


            _insertedList.AddRange(_pendingList);
            _pendingList.Clear();
        }

        public IQueryable<TempIdTable> TempIdTable
        {
            get
            {
                InsertPending();
                return _context.Set<TempIdTable>().Where(x => x.PrivateId == TempTableIndex.Value);
            }
        }

        IQueryable<T> FilterById<T>(IQueryable<T> query, Expression<Func<T, int>> keySelector) //For DelayedBatchIncludeExtension's reflector code
        {
            return _insertedList.Count + _pendingList.Count <= TempTableThreshold
                           ? LightFilter(query, keySelector, _insertedList.Concat(_pendingList))
                           : query.Join(TempIdTable, keySelector, t => t.RefId, (q, t) => q);
        }

        public IQueryable<T> Filter<T>(IQueryable<T> query, Expression<Func<T, int?>> keySelector)
        {
            return _insertedList.Count + _pendingList.Count <= TempTableThreshold
                ? LightFilter(query, keySelector, _insertedList.Concat(_pendingList).Select(x => (int?)x))
                : query.Join(TempIdTable, keySelector, t => t.RefId, (q, t) => q);
        }


        static IQueryable<T> LightFilter<T, TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> keys)
        {
            var keyList = keys.Distinct().ToList();

            if (!keyList.Any())
                return query.Where(x => false);

            Expression<Func<ICollection<TKey>>> values = () => keyList;

            var predicateBody = Expression.Call(values.Body, typeof(ICollection<TKey>).GetMethod("Contains", new[] { typeof(TKey) }), keySelector.Body);

            var l = Expression.Lambda<Func<T, bool>>(predicateBody, keySelector.Parameters);

            return query.Where(l);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && OpenedConnection.State == System.Data.ConnectionState.Open && _toDropTempTable)
            {
                var query = $@"if OBJECT_ID('tempdb..#TempIdTable') IS NOT NULL DROP TABLE #TempIdTable";
                _context.Database.ExecuteSqlRaw(query);
            }
        }

    }
}
