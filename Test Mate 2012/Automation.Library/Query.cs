using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Library
{
    public class Query : SqlServer
    {
        public static Hashtable GetMyLastRunResults(string machineName)
        {
            using (
                var connection = SqlServer.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "select fMasterRunId from rsmast where fStartTime = (select MAX(fStartTime) from rsmast where fmachinename = '" +
                    machineName + "')";
                string masterrunid;
                try
                {
                    masterrunid = command.ExecuteScalar().ToString();
                }
                catch (Exception exception)
                {
                    throw new ArgumentException("Invalid User", exception);
                }

                var totalTestcasecommand = connection.CreateCommand();
                totalTestcasecommand.CommandText =
                    "Select COUNT(fOutCome) from rsitem where fMasterRunId='" + masterrunid + "'";
                var passedTestcasecommand = connection.CreateCommand();
                passedTestcasecommand.CommandText =
                    "Select COUNT(fOutCome) from rsitem where fMasterRunId='" + masterrunid + "' and fOutCome='Passed'";
                var failedTestcasecommand = connection.CreateCommand();
                failedTestcasecommand.CommandText =
                    "Select COUNT(fOutCome) from rsitem where fMasterRunId='" + masterrunid + "' and fOutCome='Failed'";
                var roicommand = connection.CreateCommand();
                roicommand.CommandText =
                    "Select fRoi from rsmast where fMasterRunId='" + masterrunid + "'";
                var dataHashtable = new Hashtable();
                try
                {
                    var totaltestcase = totalTestcasecommand.ExecuteScalar().ToString();
                    var passedtestcase = passedTestcasecommand.ExecuteScalar().ToString();
                    var failedtestcase = failedTestcasecommand.ExecuteScalar().ToString();
                    var roi = roicommand.ExecuteScalar().ToString();
                    dataHashtable.Add("TotalTC", totaltestcase);
                    dataHashtable.Add("Passed", passedtestcase);
                    dataHashtable.Add("Failed", failedtestcase);
                    dataHashtable.Add("Roi", roi);
                }
                catch (Exception exception)
                {
                    LoggerUtil.LogMessageToFile("Error encountered while getting the last run results is : " + exception.ToString());
                    throw new ArgumentException("Invalid User", exception);
                }
                finally
                {
                    connection.Close();}
                    LoggerUtil.LogMessageToFile("Getting last run results was successful");
                
                return dataHashtable;
            }
        }

        public static List<Dictionary<DateTime, double>> GetLastRunResults(string productName, int dt, string userName = null)
        {
            List<Dictionary<DateTime, double>> combinedResults = new List<Dictionary<DateTime, double>>();
            Dictionary<DateTime, double> passedDictionary = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> failedDictionary = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> roiDictionary = new Dictionary<DateTime, double>();

            string datePicked = DateTime.Now.AddDays(-dt).ToString("yyyy-MM-dd");
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd"); 

            using (
                var connection = SqlServer.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                if (userName == null && dateNow != datePicked)
                {
                    command.CommandText =
                        "SELECT fStartTime,fRunBy,ftcTotal,ftcExecuted,ftcPassed,ftcFailed,fRoi" +
                        " FROM [TestAutomation].[dbo].[rsmast]" +
                        " where fstartTime >= '" + datePicked + "'" +
                       " and fProductName='" + productName + "'";
                }
                else if (dateNow == datePicked && userName ==null)
                {
                    command.CommandText =
                        "SELECT fStartTime,fRunBy,ftcTotal,ftcExecuted,ftcPassed,ftcFailed,fRoi" +
                        " FROM [TestAutomation].[dbo].[rsmast]" +
                        " where fstartTime >= '"+dateNow+"'" +
                       " and fProductName='" + productName + "'";
                }
                else
                {
                    command.CommandText =
                        "SELECT fStartTime,fRunBy, ftcTotal,ftcExecuted,ftcPassed,ftcFailed,fRoi" +
                        " FROM [TestAutomation].[dbo].[rsmast]" +
                        " where fstartTime >= '" + datePicked + "'" +
                        " and fProductName='" + productName + "'" +
                        " and fRunBy = '" + userName + "'";
                }

                try
                {
                    SqlDataReader sReader = command.ExecuteReader();

                    while (sReader.Read())
                    {
                        passedDictionary.Add(Convert.ToDateTime(sReader.GetValue(0)), Convert.ToDouble(sReader.GetValue(4)));
                        failedDictionary.Add(Convert.ToDateTime(sReader.GetValue(0)), Convert.ToDouble(sReader.GetValue(5)));
                        roiDictionary.Add(Convert.ToDateTime(sReader.GetValue(0)), Convert.ToDouble(sReader.GetValue(6)));
                    }

                    combinedResults.Add(passedDictionary);
                    combinedResults.Add(failedDictionary);
                    combinedResults.Add(roiDictionary);
                }
                catch (Exception exception)
                {
                    throw new ArgumentException("Invalid User", exception);
                }
                finally
                {
                    connection.Close();
                }
                return combinedResults;
            }
        }

        public static List<string> GetRunByUsers(string productName)
        {
            List<string> users = new List<string>();

            using (
                var connection = SqlServer.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText =
                    "Select distinct fRunBy " +
                    " From [TestAutomation].[dbo].[rsmast] where fProductName = '" + productName + "'";
                try
                {
                    SqlDataReader sReader = command.ExecuteReader();

                    while (sReader.Read())
                    {
                        users.Add(sReader.GetString(0));
                    }
                    users.Add("All");
                }
                catch (Exception exception)
                {
                    throw new ArgumentException("Invalid User", exception);
                }
                finally { connection.Close(); }
                return users;
            }
        }

        public static Dictionary<string, string> GetCompatibilityMatrix(string productName)
        {
            Dictionary<string, string> cMatrix = new Dictionary<string, string>();
            SqlConnection sCon = new SqlConnection();
            try
            {
                sCon = SqlServer.GetConnection();
                using (sCon)
                {
                    SqlCommand command = new SqlCommand(
                      "SELECT * from Compatibility where Product='" + productName + "';",
                      sCon);
                    sCon.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            cMatrix.Add(reader.GetName(i), reader.GetValue(i).ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerUtil.LogMessageToFile("Exception caught while Getting the compatibility matrix for product '" + productName + "' from sql server is : " + ex.ToString());
                sCon.Close();
                throw new Exception("Server Connection Error");
            }
            finally
            {
                sCon.Close();
            }
            return cMatrix;
        }

        public static DataTable GetCompatibilityMatrix()
        {
            DataTable dt = new DataTable();
             SqlConnection sCon  = new SqlConnection();
            try
            {
                 sCon = Query.GetConnection();
                SqlCommand sCmd = new SqlCommand("select * from Compatibility", sCon);
                SqlDataAdapter sData = new SqlDataAdapter(sCmd);
                sData.Fill(dt);
            }
            catch (Exception ex)
            {
                LoggerUtil.LogMessageToFile("Exception caught while Getting the compatibility matrix table from sql server is : " + ex.ToString());
            }
            finally { sCon.Close(); }
            return dt;
        }
    }
}
