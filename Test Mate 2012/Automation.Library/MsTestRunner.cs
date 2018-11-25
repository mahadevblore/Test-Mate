using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Management;
using System.Management.Automation;
using System.Threading;
using System.Collections.ObjectModel;

namespace Automation.Library
{
    public class MsTestRunner : LoggerUtil
    {
        public class ShortTrxEntity
        {
            public string TestName { get; set; }
            public string TestOutcome { get; set; }
        }

        public static void KillMsTest()
        {
            try
            {
                Process pKill = Process.GetProcesses().Single(x => x.ProcessName.Contains("MSTest"));
                LogMessageToFile("Trying to find the process MsTest.exe : " + pKill.MachineName + "  " + pKill.ProcessName);
                pKill.Kill();
                LogMessageToFile("Successfully Killed the MsTest.exe");
            }
            catch (Exception ex)
            {
                LogMessageToFile("Exception caught while killing MSTest is : " + ex.ToString());
            }
        }

        public static string GetLatestResultspath(string source)
        {
            LogMessageToFile("Getting Latest results path");
            var directories = Directory.GetDirectories(source);
            Array.Sort(directories);
            LogMessageToFile("The Result Path is : " + directories[directories.Length - 1] + "\\Results.trx");
            return directories[directories.Length - 1] + "\\Results.trx";
        }

        public static void Run(string dllName,string dllLocation, string category, string module, List<string> testCase)
        {
            LogMessageToFile("Killing MsTest.exe");
            KillMsTest();
            string path = ConfigurationManager.AppSettings["TestRunBatchFileLocation"] + "test" + DateTime.Now.ToString("hhmmss") + ".bat";
            var contentBuilder = new StringBuilder();
            string msTestPath = null;

            LoggerUtil.LogMessageToFile("Finding MSTest.exe on the system");

            if (!File.Exists(@"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe"))
            {
                string[] msTestPaths = null;
                try
                {
                    string temp = CommandUtils.GetMsTestPath().Split(new string[] { "Directory of" },
                        System.StringSplitOptions.None)[1];
                    LogMessageToFile("Split result of mstest path is : " + temp);
                    msTestPaths = temp.Split(new string[] { @"\IDE" }, System.StringSplitOptions.None);
                    msTestPath = msTestPaths[0].Trim() + @"\IDE\";
                    LogMessageToFile("MsTestPath found using enumeration is File is " + msTestPath);
                }
                catch (Exception ex)
                {
                    LogMessageToFile("Exception caught at finding MSTest.exe path is : " + ex.ToString());
                }
                finally
                {
                    if (msTestPath == null)
                    {
                        LogMessageToFile("Throwing Custom Exception for mstestpath to be null.");
                        throw new CustomException("Please Install Test Agent or Visual Studio to run test cases.");
                    }
                }
            }
            else
            {
                msTestPath = @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\";
            }
            LoggerUtil.LogMessageToFile("The MSTest executable path is : " + msTestPath);
           
            contentBuilder.AppendLine(@"cd " + msTestPath);
            contentBuilder.Append(@"MSTest.exe /testcontainer:""");
            string otLoc = OTGen.CreateOrderedTest(testCase, dllName, dllLocation, ConfigurationManager.AppSettings["XmlSchema"],
               ConfigurationManager.AppSettings["MSType"], dllLocation.Substring(0, dllLocation.LastIndexOf('\\')));
            contentBuilder.Append(otLoc + "\"");
            contentBuilder.Append(" /testsettings:\"");
            string settingFilePath =  @"C:\Automation\Automation.testsettings";
            string loc = CreateNewTestResultpath(ConfigurationManager.AppSettings["TrxFileLocation"]).ToString();
            contentBuilder.Append(settingFilePath + "\"");
           
            contentBuilder.Append(" /resultsfile:\"");

            contentBuilder.Append(loc+"\"");

            LogBatFileOutput(contentBuilder, path);

            CommandUtils.DeleteTestBatFiles();

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(contentBuilder.ToString());
                writer.Close();
            }

            var start = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            DateTime dt = DateTime.Now;
            using (Process process = Process.Start(start))
            {
                if (process != null)
                {
                    System.Threading.Thread.Sleep(2000);
                    dt = DateTime.Now;
                    LogMessageToFile("MSTest triggered without waiting for the process");
                }
            }
        }

        public static void RunExtended(string dllName, string dllLocation, string category, string module, List<string> testCase)
        {
            KillMsTest();
            string path = ConfigurationManager.AppSettings["TestRunBatchFileLocation"] + "test" + DateTime.Now.ToString("hhmmss") + ".bat";
            var contentBuilder = new StringBuilder();
            string msTestPath = null;

            LoggerUtil.LogMessageToFile("Finding MSTest.exe on the system");

            if (!File.Exists(@"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe"))
            {
                string[] msTestPaths = null;
                try
                {
                    msTestPaths = CommandUtils.GetMsTestPath().Split(new string[] { "Directory of" },
                        System.StringSplitOptions.None)[1].Split(new string[] { "IDE" }, System.StringSplitOptions.None);
                }
                catch (Exception ex)
                {
                    LogMessageToFile("Exception caught at finding MSTest.exe path is : " + ex.ToString());
                }
                finally
                {
                    if (msTestPath == null || !msTestPath.Contains("Visual Studio"))
                    {
                        throw new CustomException("Please Install Test Agent or Visual Studio to run test cases.");
                    }
                    msTestPath = msTestPaths[0].Trim() + @"IDE\";
                }
            }
            else
            {
                msTestPath = @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\";
            }

            contentBuilder.AppendLine(@"cd " + msTestPath);
            contentBuilder.Append(@"MSTest.exe /testcontainer:""");
            string otLoc = OTGen.CreateOrderedTest(testCase, dllName, dllLocation, ConfigurationManager.AppSettings["XmlSchema"],
               ConfigurationManager.AppSettings["MSType"], dllLocation.Substring(0, dllLocation.LastIndexOf('\\')));
            contentBuilder.Append(otLoc + "\"");

            //if (module == "All")
            //{
            //}
            //else
            //{
            //    if (testCase == "All")
            //    {
            //        contentBuilder.Append(" /category:");
            //        contentBuilder.Append("\"" + module + "\"");
            //    }
            //    else
            //    {
            //        contentBuilder.Append(" /test:");
            //        contentBuilder.Append(testCase);
            //    }
            //}

            contentBuilder.Append(" /testsettings:\"");
            string settingFilePath = @"C:\Automation\Automation.testsettings";
            string loc = CreateNewTestResultpath(ConfigurationManager.AppSettings["TrxFileLocation"]).ToString();
            contentBuilder.Append(settingFilePath+"\"");
            //string tempLoc = loc.ToString().Replace("Results.trx", "");
            //string destFilePath = tempLoc + "Automation.testsettings";
            //contentBuilder.Append("Automation.testsettings" + "\"");
            //File.Copy(settingFilePath, destFilePath);
            contentBuilder.Append(" /resultsfile:\"");

            contentBuilder.Append(loc + "\"");
            contentBuilder.AppendLine("  ");
            contentBuilder.AppendLine("exit");

            LogBatFileOutput(contentBuilder, path);

            CommandUtils.DeleteTestBatFiles();

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(contentBuilder.ToString());

                writer.Close();
            }

            var start = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };


            using (Process process = Process.Start(start))
            {
                LogMessageToFile("MSTest triggered without waiting for the process");
                //if (process != null)
                //{
                //    using (StreamReader reader = process.StandardOutput)
                //    {
                //        string result = reader.ReadToEnd();
                //        LogMessageToFile(result);
                //    }
                //    process.WaitForExit();
                //}
            }
        }

        public static void RunDirectCall(string dllName,string dllLocation, string category, string module, List<string> testCase)
        {
            KillMsTest();
            string path = ConfigurationManager.AppSettings["TestRunBatchFileLocation"] + "test" + DateTime.Now.ToString("hhmmss") + ".bat";
            var contentBuilder = new StringBuilder();
            string msTestPath = null;

            LoggerUtil.LogMessageToFile("Finding MSTest.exe on the system");

            if (!File.Exists(@"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe"))
            {
                string[] msTestPaths = null;
                try
                {
                    msTestPaths = CommandUtils.GetMsTestPath().Split(new string[] { "Directory of" },
                        System.StringSplitOptions.None)[1].Split(new string[] { "IDE" }, System.StringSplitOptions.None);
                }
                catch (Exception ex)
                {
                    LogMessageToFile("Exception caught at finding MSTest.exe path is : " + ex.ToString());
                }
                finally
                {
                    if (msTestPath == null || !msTestPath.Contains("Visual Studio"))
                    {
                        throw new CustomException("Please Install Test Agent or Visual Studio to run test cases.");
                    }
                    msTestPath = msTestPaths[0].Trim() + @"IDE\";
                }
            }
            else
            {
                msTestPath = @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\";
            }

            contentBuilder.AppendLine(@"cd " + msTestPath);
            contentBuilder.Append(@"MSTest.exe /testcontainer:""");
           string otLoc= OTGen.CreateOrderedTest(testCase, dllName,dllLocation,ConfigurationManager.AppSettings["XmlSchema"],
                ConfigurationManager.AppSettings["MSType"], dllLocation.Substring(0,dllLocation.LastIndexOf('\\')));
           contentBuilder.Append(otLoc + "\"");

            //if (module == "All")
            //{
            //}
            //else
            //{
            //    if (testCase == "All")
            //    {
            //        contentBuilder.Append(" /category:");
            //        contentBuilder.Append("\"" + module + "\"");
            //    }
            //    else
            //    {
            //        contentBuilder.Append(" /test:");
            //        contentBuilder.Append(testCase);
            //    }
            //}
           contentBuilder.Append(" /testsettings:\"");
           string settingFilePath = @"C:\Automation\Automation.testsettings";
           string loc = CreateNewTestResultpath(ConfigurationManager.AppSettings["TrxFileLocation"]).ToString();
           contentBuilder.Append(settingFilePath + "\"");
           //string tempLoc = loc.ToString().Replace("Results.trx", "");
           //string destFilePath = tempLoc + "Automation.testsettings";
           //contentBuilder.Append("Automation.testsettings"+"\"");
           //File.Copy(settingFilePath, destFilePath);
           contentBuilder.Append(" /resultsfile:\"");
            //checkin comment
           contentBuilder.Append(loc+"\"");
            contentBuilder.AppendLine("  ");
            //contentBuilder.AppendLine("taskkill /F /IM CdcSmartClientContainer.exe /T");
            contentBuilder.AppendLine("exit");

            LogBatFileOutput(contentBuilder, path);

            CommandUtils.DeleteTestBatFiles();

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(contentBuilder.ToString());

                writer.Close();
            }

            var start = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };


            using (Process process = Process.Start(start))
            {
                LogMessageToFile("MSTest triggered without waiting for the process");
                //if (process != null)
                //{
                //    //using (StreamReader reader = process.StandardOutput)
                //    //{
                //    //    string result = reader.ReadToEnd();
                //    //    LogMessageToFile(result);
                //    //}
                //    process.WaitForExit();
                //}
            }
        }

        private static object CreateNewTestResultpath(string appSettingPath)
        {
            LogMessageToFile("Creating New result path : " + appSettingPath);
            var path = appSettingPath;
            var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            string[] userName = null;
            if (windowsIdentity != null)
            {
                userName = windowsIdentity.Name.Split('\\');
            }
            if (userName != null)
            {
                String directoryName = userName[1] + "_" + Environment.MachineName + "_" +
                                       DateTime.Now.ToString("yyyyMMddHHmm");

                path = CreateUniqueDirectory(path, directoryName);
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            LogMessageToFile("New Result path created is : " + path + "\\Results.trx");
            return path + "\\Results.trx";
        }

        private static string CreateUniqueDirectory(string directoryPath, string directoryName)
        {
            int fileNumber = 1;

            //Generate new name
            while (Directory.Exists(Path.Combine(directoryPath, directoryName + "_" + fileNumber)))
                fileNumber++;
            LogMessageToFile("Unique directory created for the curent run is : " + directoryPath + "\\" + directoryName + "_" + fileNumber);
            return directoryPath + "\\" + directoryName + "_" + fileNumber;
        }

        public static List<ShortTrxEntity> GetShortResults()
        {
            string source = ConfigurationManager.AppSettings["TrxFileLocation"];
            LogMessageToFile("The source path for GetShortResults is : " + source);
            source = GetLatestResultspath(source);
            var lstTrxEntity = new List<ShortTrxEntity>();
            LogMessageToFile("Trying to load the TRX source");
            XDocument xDoc = XDocument.Load(source);
            LogMessageToFile("TRX Source loaded");
            XNamespace defaultNs = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            var unitTestResultNode = (from data in xDoc.Descendants(defaultNs + "Results")
                                      select data).Descendants(defaultNs + "UnitTestResult").ToList();

            var recordSet = (from src in unitTestResultNode
                             select new
                             {
                                 TestName = src.Attribute("testName").Value,
                                 TestOutcome = src.Attribute("outcome").Value
                             });

            foreach (var record in recordSet)
            {
                bool flag = false;
                foreach (ShortTrxEntity entity in lstTrxEntity)
                {
                    if (entity.TestName == record.TestName)
                    {
                        flag = true;
                        if (entity.TestOutcome.ToUpper() == "failed".ToUpper())
                        {
                            entity.TestOutcome = record.TestOutcome;
                        }
                    }
                }
                if (flag == false)
                {
                    lstTrxEntity.Add(new ShortTrxEntity
                    {
                        TestName = record.TestName,
                        TestOutcome = record.TestOutcome,
                    });
                }
            }
            LogMessageToFile("GetShortResults was successfully completed");
            return lstTrxEntity;
        }

        public static void UpdateResults(string productName, string productBuildNumber, string testBuildNumber,
            DateTime starttime, DateTime endtime)
        {
            string source = ConfigurationManager.AppSettings["TrxFileLocation"];
            LogMessageToFile("The source path for GetShortResults is : " + source);
            source = GetLatestResultspath(source);
            if (!File.Exists(source))
            {

            }
            else
            {
                LogMessageToFile("Trying to load the source");
                XDocument xDoc = XDocument.Load(source);
                LogMessageToFile("Source loaded");
                XNamespace defaultNs = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

                var dataHashtable = new Hashtable();
                var testRunNode = (from data in xDoc.Descendants(defaultNs + "TestRun")
                                   select data).ToList();
                var masterRecordSet1 = (from src in testRunNode
                                        select new
                                        {
                                            testRunId = src.Attribute("id").Value,
                                            testRunName = src.Attribute("name").Value,
                                            testRunUser = src.Attribute("runUser").Value
                                        });
                foreach (var record in masterRecordSet1)
                {
                    dataHashtable.Add("ID", record.testRunId);
                    dataHashtable.Add("FMASTRUNID", record.testRunName);
                    dataHashtable.Add("FRUNUSER", record.testRunUser);
                }
                var resultSummaryNode = (from data in xDoc.Descendants(defaultNs + "ResultSummary")
                                         select data).ToList();
                var resultSummaryRecordSet1 = (from src in resultSummaryNode
                                               select new
                                               {
                                                   outcome = src.Attribute("outcome").Value
                                               });
                foreach (var record in resultSummaryRecordSet1)
                {
                    dataHashtable.Add("FOUTCOME", record.outcome);
                }
                resultSummaryNode = (from data in xDoc.Descendants(defaultNs + "ResultSummary")
                                     select data).Descendants(defaultNs + "Counters").ToList();
                var resultSummaryRecordSet2 = (from src in resultSummaryNode
                                               select new
                                               {
                                                   totalTestCases = src.Attribute("total").Value,
                                                   totalTestCasesExecuted = src.Attribute("executed").Value,
                                                   totalTestCasesPassed = src.Attribute("passed").Value,
                                                   totalTestCasesError = src.Attribute("error").Value,
                                                   totalTestCasesFailed = src.Attribute("failed").Value,
                                                   totalTestCasesTimeout = src.Attribute("timeout").Value,
                                                   totalTestCasesAborted = src.Attribute("aborted").Value,
                                                   totalTestCasesInconclusive = src.Attribute("inconclusive").Value
                                               });
                foreach (var record in resultSummaryRecordSet2)
                {
                    dataHashtable.Add("FTCTOTAL", record.totalTestCases);
                    dataHashtable.Add("FTCEXECUTED", record.totalTestCasesExecuted);
                    dataHashtable.Add("FTCPASSED", record.totalTestCasesPassed);
                    dataHashtable.Add("FTCERROR", record.totalTestCasesError);
                    dataHashtable.Add("FTCFAILED", record.totalTestCasesFailed);
                    dataHashtable.Add("FTCTIMEOUT", record.totalTestCasesTimeout);
                    dataHashtable.Add("FTCABORTED", record.totalTestCasesAborted);
                    dataHashtable.Add("FTCINCONCLUSIVE", record.totalTestCasesInconclusive);
                }
                using (var connection = SqlServer.GetConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    double roiFactor = ProductConfiguration.GetProductRoiFactor(productName);
                    var roi = Convert.ToDouble(dataHashtable["FTCPASSED"]) * roiFactor;
                    command.CommandText =
                        "INSERT INTO rsmast (fMasterRunId, fProductName, fProductBuildNumber, fTestBuildNumber, fOutCome, fRunBy, fStartTime, fEndTime, ftcTotal, ftcExecuted, ftcPassed, ftcFailed, ftcTimeout, ftcAborted, ftcError, ftcInconclusive, ftcUrl, ftcbrowser, ftcExePath, fRoi)" +
                        "VALUES ('" +
                        dataHashtable["FMASTRUNID"] + "','" +
                        productName + "','" +
                        productBuildNumber + "','" +
                        testBuildNumber + "','" +
                        dataHashtable["FOUTCOME"] + "','" +
                        dataHashtable["FRUNUSER"] + "','" +
                        starttime + "','" +
                        endtime + "','" +
                        dataHashtable["FTCTOTAL"] + "','" +
                        dataHashtable["FTCEXECUTED"] + "','" +

                        dataHashtable["FTCPASSED"] + "','" +
                        dataHashtable["FTCFAILED"] + "','" +

                        dataHashtable["FTCTIMEOUT"] + "','" +
                        dataHashtable["FTCABORTED"] + "','" +
                        dataHashtable["FTCERROR"] + "','" +
                        dataHashtable["FTCINCONCLUSIVE"] + "','" +
                        "url" + "','" +
                        "browser" + "','" +
                        "exepath" + "','" +
                        roi + "');";

                    try
                    {
                        command.ExecuteNonQuery();
                        connection.Close();
                        LogMessageToFile("Updating results to SQL was successfully completed.");
                    }
                    catch (Exception ex)
                    {
                        LogMessageToFile("Exception caught while updating results to SQL Server is : " + ex.ToString());
                        throw new Exception("failed to update rsmast table");
                    }
                }

                var detailedTestRunNode = (from data in xDoc.Descendants(defaultNs + "Results")
                                           select data).Descendants(defaultNs + "UnitTestResult").ToList();

                var detailedTestrecordSet1 = (from src in detailedTestRunNode
                                              select new
                                              {
                                                  TestRunID = src.Attribute("testId").Value,
                                                  testcaseRunId = src.Attribute("testId").Value,
                                                  TestName = src.Attribute("testName").Value,
                                                  TestOutcome = src.Attribute("outcome").Value,
                                                  EnvirnomentName = src.Attribute("computerName").Value,
                                                  //Duration = src.Attribute("duration").Value,
                                                  StartTime = src.Attribute("startTime").Value,
                                                  EndTime = src.Attribute("endTime").Value,
                                                  Message = src
                                                      .Descendants(defaultNs + "Output")
                                                      .Descendants(defaultNs + "ErrorInfo")
                                                      .Descendants(defaultNs + "Message"),
                                                  StackTrace = src
                                                      .Descendants(defaultNs + "Output")
                                                      .Descendants(defaultNs + "ErrorInfo")
                                                      .Descendants(defaultNs + "StackTrace")
                                              });

                foreach (var record in detailedTestrecordSet1)
                {
                    using (var connection = SqlServer.GetConnection())
                    {
                        connection.Open();
                        var command0 = connection.CreateCommand();
                        command0.CommandText = "select fOutCome from rsitem where fMasterRunId='" +
                                               dataHashtable["FMASTRUNID"] +
                                               "' and  fTestRunId='" + record.TestRunID + "'";
                        string result = null;
                        try
                        {
                            result = command0.ExecuteScalar().ToString();
                        }
                        catch (NullReferenceException)
                        {
                            var command1 = connection.CreateCommand();
                            command1.CommandText =
                                "INSERT INTO rsitem (fMasterRunId, fTestRunId, fOutCome)" +
                                "VALUES ('" +
                                dataHashtable["FMASTRUNID"] + "','" +
                                record.TestRunID + "','" +
                                record.TestOutcome + "');";
                            try
                            {
                                command1.ExecuteNonQuery();
                            }
                            catch (Exception)
                            {
                                throw new Exception("Failed to update rsitem table");
                            }
                        }
                        if (result != null &&
                            (result.ToUpper() != "Failed".ToUpper() && record.TestOutcome.ToUpper() == "Failed".ToUpper()))
                        {
                            var command2 = connection.CreateCommand();
                            command2.CommandText =
                                "update rsitem set fOutCome = 'Failed' where fMasterRunId='" +
                                dataHashtable["FMASTRUNID"] + "' and fTestRunId='" + record.TestRunID + "'";
                        }
                        var command3 = connection.CreateCommand();
                        command3.CommandText = "update rsmast set fMachineName = '" + record.EnvirnomentName +
                                               "' where fMasterRunId='" +
                                               dataHashtable["FMASTRUNID"] + "'";
                        try
                        {
                            command3.ExecuteNonQuery();
                        }
                        catch (Exception)
                        {
                            throw new Exception("Failed to update fMachineName in rsmast table");
                        }
                        var duration = Convert.ToDateTime(record.EndTime) - Convert.ToDateTime(record.StartTime);
                        var command = connection.CreateCommand();
                        command.CommandText =
                            "INSERT INTO rsdetailtestview (fMasterRunId, fTestRunId, ftcName, ftcComputerName, ftcDuration, ftcStartTime, ftcEndTime, ftcOutcome, ftcDebugTrace, ftcStackTrace)" +
                            "VALUES ('" +
                            dataHashtable["FMASTRUNID"] + "','" +
                            record.TestRunID + "','" +
                            record.TestName + "','" +
                            record.EnvirnomentName + "','" +
                            duration.TotalMinutes + "','" +
                            record.StartTime + "','" +
                            record.EndTime + "','" +
                            record.TestOutcome + "','" +
                            record.Message + "','" +
                            record.StackTrace + "');";
                        try
                        {
                            command.ExecuteNonQuery();
                            LogMessageToFile("Updating results to SQL was successfully completed.");
                        }
                        catch (Exception ex)
                        {
                            LogMessageToFile("Exception caught while updating results to SQL Server is : " + ex.ToString());
                            throw new Exception("Failed to update rsdetailtestview table");
                        }
                        connection.Close();
                    }
                }

                LogMessageToFile("Updating Splog");
                SplogUtils spUtils = new SplogUtils();
                spUtils.splogPathLocal = LoggerUtil.GetTempPath();
                spUtils.splogfileName = productName + "_" + dataHashtable["FMASTRUNID"].ToString().Replace("-", "_").Replace(":", "_") + ".splog";
                LogMessageToFile("Splog file name is : " + spUtils.splogfileName);
                spUtils.CreateXMLFile(productName, testBuildNumber);
                LogMessageToFile("Created Splog file");
                spUtils.Generate(dataHashtable, productName, productBuildNumber, testBuildNumber, "", "");
                LogMessageToFile("Generated splog for the current run");
                spUtils.ReportToSplunk(productName, testBuildNumber);
            }
        }

        public static Hashtable GetLastRunResults(string productName)
        {
            LogMessageToFile("Getting last run results");
            using (
                var connection = SqlServer.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "select fMasterRunId from rsmast where fStartTime = (select MAX(fStartTime) from rsmast where fProductName = '" +
                    productName + "')";
                string masterrunid;
                try
                {
                    masterrunid = command.ExecuteScalar().ToString();
                }
                catch (Exception exception)
                {
                    throw new ArgumentException("Cannot Connect to DataBase", exception);
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
                    throw new ArgumentException("Invalid User", exception);
                }
                connection.Close();
                return dataHashtable;
            }
        }

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
                    LogMessageToFile("Error encountered while getting the last run results is : " + exception.ToString());
                    throw new ArgumentException("Invalid User", exception);
                }
                connection.Close();
                LogMessageToFile("Getting last run results was successful");
                return dataHashtable;
            }
        }

        private static void ProcessLogic(ProcessStartInfo start)
        {

            DateTime dt = DateTime.Now;
            using (Process process = Process.Start(start))
            {
                if (process != null)
                {
                    System.Threading.Thread.Sleep(2000);
                    dt = DateTime.Now;
                    //using (StreamReader reader = process.StandardOutput)
                    //{
                    //    string result = reader.ReadToEnd();
                    //}
                    //process.WaitForExit();
                    LogMessageToFile("MSTest triggered without waiting for the process");
                }
            }

            try
            {
                Process pr = null;
                int i = 0;
                do
                {
                    pr = Process.GetProcesses().Single(x => x.ProcessName.Contains("MSTest"));
                    dt = pr.StartTime;
                    i++;
                } while (pr == null && i != 4);

                while (!pr.HasExited)
                {
                    System.Threading.Thread.Sleep(4000);
                }

                Process[] prs = Process.GetProcesses();
                prs = prs.Where(x => x.StartTime > dt && x.StartInfo.UserName == Environment.UserName).ToArray<Process>();
                foreach (Process prKill in prs)
                {
                    prKill.Kill();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static void PowerShellSolution(string dllLocation, string module, string testCase, StringBuilder contentBuilder, string msTestPath)
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                // this script has a sleep in it to simulate a long running script
                //PowerShellInstance.AddScript("start-sleep -s 7; get-service");

                PowerShellInstance.AddScript("$fs = New-Object -ComObject Scripting.FileSystemObject");
                PowerShellInstance.AddScript("$f = $fs.GetFile(\"" + msTestPath + "\")");
                PowerShellInstance.AddScript("$mstestPath = $f.shortpath");
                PowerShellInstance.AddScript("$dllLocation='\"" + dllLocation + "\"'");
                PowerShellInstance.AddScript("$results=\"" + CreateNewTestResultpath(ConfigurationManager.AppSettings["TrxFileLocation"]) + "\"");
                if (module == "All")
                { //place holder for running multiple categories
                }
                else
                {
                    if (testCase == "All")
                    {
                        PowerShellInstance.AddScript("$module=\"" + module + "\"");
                        PowerShellInstance.AddScript("$arguments =  \"/testcontainer:\"+ $dllLocation +\" " +
                            " /category:\"+ $module +" +
                            "\" /resultsfile:\"+$results");
                    }
                    else
                    {
                        PowerShellInstance.AddScript("$test=\"" + testCase + "\"");
                        PowerShellInstance.AddScript("$arguments = \"/testcontainer:\"+$dllLocation +\"" +
                            " /test:\"+$test+" +
                            "\" /resultsfile:\"+$results");
                    }
                }
                PowerShellInstance.AddScript("Invoke-Expression \"$mstestPath $arguments\"");

                Collection<PSObject> PSOutput = PowerShellInstance.Invoke();

                // loop through each output object item
                foreach (PSObject outputItem in PSOutput)
                {
                    // if null object was dumped to the pipeline during the script then a null
                    // object may be present here. check for null to prevent potential NRE.
                    if (outputItem != null)
                    {
                        //TODO: do something with the output item 
                        // outputItem.BaseOBject
                    }
                }
            }

            contentBuilder.AppendLine("$fs = New-Object -ComObject Scripting.FileSystemObject");
            contentBuilder.AppendLine("$f = $fs.GetFile(\"" + msTestPath + "\")");
            contentBuilder.AppendLine("$mstestPath = $f.shortpath");
            contentBuilder.AppendLine("$dllLocation='\"" + dllLocation + "\"'");
            contentBuilder.AppendLine("$results=\"" + CreateNewTestResultpath(ConfigurationManager.AppSettings["TrxFileLocation"]) + "\"");
            if (module == "All")
            { //place holder for running multiple categories
            }
            else
            {
                if (testCase == "All")
                {
                    contentBuilder.AppendLine("$module=\"" + module + "\"");
                    contentBuilder.AppendLine("$arguments =  \"/testcontainer:\"+ $dllLocation +\" " +
                        " /category:\"+ $module +" +
                        "\" /resultsfile:\"+$results");
                }
                else
                {
                    contentBuilder.AppendLine("$test=\"" + testCase + "\"");
                    contentBuilder.AppendLine("$arguments = \"/testcontainer:\"+$dllLocation +\"" +
                        " /test:\"+$test+" +
                        "\" /resultsfile:\"+$results");
                }
            }
            contentBuilder.AppendLine("Invoke-Expression \"$mstestPath $arguments\"");

        }

        //private static string FindMsTest()
        //{
        //    string path = null;
        //    try
        //    {
        //        // LINQ query for all files containing the word 'MSTest.exe'. 
        //        var files = from file in
        //                        Directory.EnumerateFiles(@Environment.SystemDirectory.Split(new string[] { @"Windows" }, System.StringSplitOptions.None)[0])
        //                    where file.ToLower().Contains("MSTest.exe")
        //                    select file;

        //        foreach (var file in files)
        //        {
        //            path= file;
        //        }
        //    }
        //    catch (UnauthorizedAccessException UAEx)
        //    {
        //        LogMessageToFile("UnAuthorized access to file : "+UAEx.ToString());
        //        throw UAEx;
        //    }
        //    catch (PathTooLongException PathEx)
        //    {
        //        LogMessageToFile("Path Too Long for the file MsTest.exe : "+PathEx.ToString());
        //        throw PathEx;
        //    }
        //    return path;
        //}
    }
}
