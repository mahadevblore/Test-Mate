using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Security.Principal;
using System.Xml;

namespace Automation.Library
{
    public class SplogUtils : LoggerUtil
    {
        //Mandatory variables to be used while instantiating the this class
        //Temporary local path for storing the splog file.
        public string splogPathLocal;
        //Splog file name created during the test run. Must be unique so that a duplicates can be avoided while copying it to splog location.
        public string splogfileName;

        public void CreateXMLFile(string productName, string build, string suiteId = null)
        {
            LogMessageToFile("Creating XML file for Splunk Log");

            //XML Pre - Settings
            XmlDocument document = new XmlDocument();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            settings.NewLineOnAttributes = true;

            //Creating required XML Elements and setting the values
            XmlElement testElement = document.CreateElement("TestResult");
            document.AppendChild(testElement);

            testElement.SetAttribute("ProjectName", productName);
            testElement.SetAttribute("BuildNumber", build);
            if (suiteId != null)
            {
                testElement.SetAttribute("SuiteID", suiteId);
            }

            //Appending and closing the splog xml file
            document.AppendChild(testElement);
            XmlWriter writer = XmlWriter.Create(splogPathLocal + "\\" + splogfileName, settings);
            document.Save(writer);
            writer.Close();

            LogMessageToFile("XML data: " + productName + " : " + build + " : " + suiteId);
            LogMessageToFile("Splunk log path created temporarily is : " + splogPathLocal);
        }
        public bool Generate(Hashtable RunReport, string productName, string productBuildNumber, string testBuildNumber, string startTime, string endTime)
        {
            try
            {
                LogMessageToFile("Appending values into XML file for splog..");

                //Loading the splog file which was created at the beginning of the run.
                XmlDocument document = new XmlDocument();
                document.Load(splogPathLocal + "\\" + splogfileName);
                XmlElement testElement = document.DocumentElement;

                //Overall Result - Setting up xml elemetns in splog file which has to be copied to splunk location.
                testElement.SetAttribute("TestRunId", RunReport["ID"].ToString());
                testElement.SetAttribute("TestRunName", RunReport["FMASTRUNID"].ToString());
                testElement.SetAttribute("StartTime", startTime);
                testElement.SetAttribute("EndTime", endTime);
                if (RunReport["FRUNUSER"].ToString().ToUpper().Contains("SWG\\"))
                {
                    testElement.SetAttribute("User", RunReport["FRUNUSER"].ToString().ToUpper().Split((new string[] { "SWG\\" }), StringSplitOptions.None)[1]);
                }
                else
                {
                    testElement.SetAttribute("User", RunReport["FRUNUSER"].ToString());
                }
                testElement.SetAttribute("Outcome", RunReport["FOUTCOME"].ToString());
                testElement.SetAttribute("Total", RunReport["FTCTOTAL"].ToString());
                testElement.SetAttribute("Executed", RunReport["FTCEXECUTED"].ToString());
                testElement.SetAttribute("Passed", RunReport["FTCPASSED"].ToString());
                testElement.SetAttribute("Failed", RunReport["FTCFAILED"].ToString());
                testElement.SetAttribute("Inconclusive", RunReport["FTCINCONCLUSIVE"].ToString());
                testElement.SetAttribute("Aborted", RunReport["FTCABORTED"].ToString());
                testElement.SetAttribute("Error", RunReport["FTCERROR"].ToString());
                testElement.SetAttribute("TimeOut", RunReport["FTCTIMEOUT"].ToString());

                //Appending the values to splog file.
                document.AppendChild(testElement);
                document.Save(splogPathLocal + "\\" + splogfileName);

                //Logging all the values into logger file.
                LogMessageToFile("The Execution report :");
                LogMessageToFile("Test Run Id is :" + RunReport["ID"].ToString());
                LogMessageToFile("TestRunName is :" + RunReport["FMASTRUNID"].ToString());
                LogMessageToFile("Start Time of testrun is :" + startTime);
                LogMessageToFile("End Time of the testrun is :" + endTime);
                LogMessageToFile("User who is triggered the run is :" + RunReport["FRUNUSER"].ToString());
                LogMessageToFile("The Outcome of the Test Execution is : " + RunReport["FOUTCOME"].ToString());
                LogMessageToFile("Total Test cases is :" + RunReport["FTCTOTAL"].ToString());
                LogMessageToFile("Total Test cases executed is :" + RunReport["FTCEXECUTED"].ToString());
                LogMessageToFile("Total Test cases passed is :" + RunReport["FTCPASSED"].ToString());
                LogMessageToFile("Total Test cases failed is :" + RunReport["FTCFAILED"].ToString());
                LogMessageToFile("Total Test cases inconclusive is :" + RunReport["FTCINCONCLUSIVE"].ToString());
                LogMessageToFile("Total Test cases aborted is :" + RunReport["FTCABORTED"].ToString());
                LogMessageToFile("Total Test cases error is :" + RunReport["FTCERROR"].ToString());
                LogMessageToFile("Total Test cases timedout is :" + RunReport["FTCTIMEOUT"].ToString());
                LogMessageToFile("End of Splog file logging");
            }
            catch (Exception exe)
            {
                return false;
            }
            return true;
        }

        public void ReportToSplunk(string projName, string buildNumber)
        {
            LogMessageToFile("Reporting the results ");

            //splog path taken from App.Config file.
            string splogLocation = ConfigurationManager.AppSettings["SplunkLocation"].ToString() + "\\" + projName.ToString() + "\\";
            
            //Checking whether a directory with build name already exists in splog location.
            //Creates directory if not exists and then copies the file.
            if (Directory.Exists(splogLocation + "\\" + buildNumber))
            {
                File.Copy(splogPathLocal + "\\" + splogfileName, splogLocation + "\\" + buildNumber + "\\" + splogfileName);
                LogMessageToFile("Copied file : " + splogfileName + " to the existing location : " + splogLocation + "\\" + buildNumber);
            }
            else
            {
                Directory.CreateDirectory(splogLocation + "\\" + buildNumber);
                File.Copy(splogPathLocal + "\\" + splogfileName, splogLocation + "\\" + buildNumber + "\\" + splogfileName, false);
                LogMessageToFile("Copied file : " + splogfileName + " to newly created location : " + splogLocation + "\\" + buildNumber);
            }
        }
    }
}
