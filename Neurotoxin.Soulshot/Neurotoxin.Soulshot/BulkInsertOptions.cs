using System;
using System.Data.SqlClient;

namespace EPAM.ReserveAIR.Shared.Repositories
{
    public class BulkInsertOptions
    {
        public SqlBulkCopyOptions CopyOptions { get; private set; }
        public int PageSize { get; private set; }
        public bool DisableIndexes { get; private set; }
        public int Timeout { get; private set; }

        public static readonly BulkInsertOptions Default = new BulkInsertOptions();

        public BulkInsertOptions(SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default, int pageSize = 500000, bool disableIndexes = false, int timeout = 6000)
        {
            CopyOptions = copyOptions;
            PageSize = pageSize;
            DisableIndexes = disableIndexes;
            Timeout = timeout;
        }
    }
}