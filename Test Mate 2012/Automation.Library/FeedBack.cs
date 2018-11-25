using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Library
{
    public class FeedBack : LoggerUtil
    {

        public bool CheckForFeedback()
        {
            using (var connection = SqlServer.GetConnection())
            {
                connection.Open();
               
                var command = connection.CreateCommand();
                command.CommandText =
                    "Select * from feedback where  machineName='" + Environment.MachineName + "' and userName='"+ Environment.UserName+"'";

                try
                {
                    SqlDataReader sReader = command.ExecuteReader();
                    LogMessageToFile("Querying SQL result was successfully completed.");
                    LogMessageToFile("SqlDataReader has rows value is : "+sReader.HasRows.ToString());
                    return sReader.HasRows;
                }
                catch (Exception ex)
                {
                    LogMessageToFile("Exception caught while querying the sql results : " + ex.ToString());
                    throw new Exception("Failed To Check IF Feedback Is Already Given");
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void InsertTheFeedback(bool opinion)
        {
            using (var connection = SqlServer.GetConnection())
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO feedback (machineName,userName,opinion) Values('" + Environment.MachineName 
                    + "','" + Environment.UserName + "'," + Convert.ToInt32(opinion)+")";

                try
                {
                    command.ExecuteNonQuery();
                    LogMessageToFile("Insertion Operation was successfully completed.");
                }
                catch (Exception ex)
                {
                    LogMessageToFile("Exception caught while inserting the data : " + ex.ToString());
                    throw new Exception("Failed To Insert Data");
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}
