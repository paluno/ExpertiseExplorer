using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseDB
{
    public static class DirectExpertiseDB
    {
        public static void addDBParameter(this IDbCommand com, string strParameterName, DbType type, object value)
        {
            IDataParameter paramNew = com.CreateParameter();
            paramNew.ParameterName = strParameterName;
            paramNew.DbType = type;
            paramNew.Value = value;
            com.Parameters.Add(paramNew);
        }
    }
}
