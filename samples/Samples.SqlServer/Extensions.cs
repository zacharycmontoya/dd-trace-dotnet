using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Samples.SqlServer
{
    public static class Extensions
    {
        public static IEnumerable<IDataRecord> AsDataRecords(this DbDataReader reader)
        {
            while (reader.Read())
            {
                yield return reader;
            }
        }

        public static IEnumerable<IDataRecord> AsDataRecords(this SqlDataReader reader)
        {
            while (reader.Read())
            {
                yield return reader;
            }
        }
    }
}
