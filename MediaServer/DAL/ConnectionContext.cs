using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using MediaServer.Exceptions;

namespace MediaServer.DAL
{
    public class ConnectionContext
    {
        #region TestADO.NET DB Connection
        // Get Tuple<bool, string> if connection to the DB is valid
        public static Tuple<bool, string> CheckConn()
        {
            // bool - successfull connection or not, string - some data like ServerVersion etc.
            try
            {
                // Get connection string from Web.config
                string connectionStr = GetConnectionString();

                // Try open connection and return
                using (var cn = new SqlConnection(connectionStr))
                {
                    cn.Open();
                    var version = cn.ServerVersion;
                    return new Tuple<bool, string>(true, $"Connectivity established! " + version);
                }
            }
            catch (Exception ex)
            {
                throw new ContextException("Exception when checking connection string!", ex);
            }
        }

        // Get connection string form Web.config
        public static string GetConnectionString()
        {
            try
            {
                System.Configuration.Configuration rootWebConfig =
               System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/MediaServer");
                System.Configuration.ConnectionStringSettings connStringSettings;
                connStringSettings =
                    rootWebConfig.ConnectionStrings.ConnectionStrings["connMediaServerDB"];
                return connStringSettings.ConnectionString;
            }
            catch (Exception ex)
            {
                throw ex;
            }


            #endregion
        }
    }
}