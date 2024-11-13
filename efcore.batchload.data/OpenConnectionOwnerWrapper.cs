using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace efcore.batchload.data
{
    public class OpenConnectionOwnerWrapper : IDisposable, IAsyncDisposable
    {
        private readonly DbContext _context;
        internal OpenConnectionOwnerWrapper(DbContext context, bool delayOpen = false)
        {
            _context = context;
            OpenedConnection = context.Database.GetDbConnection();

            if (!delayOpen)
                Open();
        }

        private bool _toClose;
        public void Open()
        {
            if (OpenedConnection.State != ConnectionState.Open)
            {
                _toClose = true;
                _context.Database.OpenConnection();
            }
        }

        public DbConnection OpenedConnection { get; }

        public SqlConnection OpenedSqlConnection => (SqlConnection)OpenedConnection;

        #region Dispose

        ~OpenConnectionOwnerWrapper()
        {
            Dispose(false);
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_toClose && OpenedConnection.State == ConnectionState.Open)
                OpenedConnection.Close();
        }


        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_toClose && OpenedConnection.State == ConnectionState.Open)
                await OpenedConnection.CloseAsync();
        }

        #endregion
    }
}
