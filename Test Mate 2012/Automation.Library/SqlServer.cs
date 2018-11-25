using System.Configuration;
using System.Data.SqlClient;

namespace Automation.Library
{
    public class SqlServer
    {
        public static SqlConnection GetConnection()
        {
            var myConnection = new SqlConnection("user id=" + ConfigurationManager.AppSettings["SQLUserName"] + ";" +
                                                 "password=" + ConfigurationManager.AppSettings["SQLPassword"] +
                                                 ";Data Source=" + ConfigurationManager.AppSettings["SQLServer"] + ";" +
                                                 "Network Library=DBMSSOCN;"+
                                                 "database=" + ConfigurationManager.AppSettings["DataBaseName"] + "; " +
                                                 "connection timeout=30");
            return myConnection;
        }
    }
}
