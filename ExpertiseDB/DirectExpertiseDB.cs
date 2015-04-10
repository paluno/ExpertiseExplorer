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
        //const string strConnectionStringName = "ExpertiseDBEntities";

        //public static DbProviderFactory backendFactory
        //{
        //    get
        //    {
        //        //try
        //        //{
        //            return
        //                DbProviderFactories.GetFactory(ConfigurationManager.ConnectionStrings[strConnectionStringName].ProviderName);
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    //// Only Debug, because it is handled elsewhere
        //        //    //log4net.LogManager.GetLogger("ExpertiseDB.DirectExpertiseDB")
        //        //    //    .Debug("Exception on getting DBProviderFactory", ex);
        //        //    throw;
        //        //}
        //    }
        //}

        //public static IDbConnection openBackendConnection()
        //{
        //    //try
        //    //{
        //        IDbConnection retVal = backendFactory.CreateConnection();
        //        retVal.ConnectionString = ConfigurationManager.ConnectionStrings[strConnectionStringName].ConnectionString;
        //        retVal.Open();
        //        return retVal;
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    //// Only Debug, because it is handled elsewhere
        //    //    //log4net.LogManager.GetLogger("ExpertiseDB.DirectExpertiseDB")
        //    //    //    .Debug("Exception on opening database", ex);
        //    //    throw;
        //    //}
        //}

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
