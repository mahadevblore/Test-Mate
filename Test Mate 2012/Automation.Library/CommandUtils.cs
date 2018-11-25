using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Automation.Library
{
    public class CommandUtils : LoggerUtil
    {
        public static string RBCopy(string sourceFilePath, string destFilepath)
        {
            Console.WriteLine("Initiating RoboCopy in command line...");
            LogMessageToFile("Initiated ROBOCOPY copy");
            string[] lines = { "net use " + "\"" + sourceFilePath + "\""+@" /user:DOMAIN\SERVICE_ACCOUNT_NAME SERVICE_ACCOUNT_PASSWORD", "ROBOCOPY " + "\"" + sourceFilePath + "\"" + " " + "\"" + destFilepath + "\"" + " /MIR /E /MT[:2]" };
            string tcmBat = GetTempPath() + "tcmRBOut.bat";
            System.IO.File.WriteAllLines(tcmBat, lines);
            Console.WriteLine("Done");
            LogMessageToFile("Successful ROBOCOPY copy");
            return CommandUtils.GetCmdOut(tcmBat, 1, sourceFilePath, destFilepath);
        }

        public static string GetCmdOut(string target, int numberOfChars, string sourceFilePath, string destFilepath)
        {
            string output2 = null;
            try
            {
                LogMessageToFile("Executing the command file : " + target);
                Console.WriteLine("Batch Command initiating...");
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = target;
                proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                proc.Start();
                System.Threading.Thread.Sleep(4000);
                LogMessageToFile("While loop validation started");

                while (true)
                {
                    try
                    {
                        if (new DirectoryInfo(sourceFilePath).GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length) == new DirectoryInfo(destFilepath).GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length))
                        {
                            LogMessageToFile("While Loop Validation Outcome is 'TRUE'");
                            break;
                        }
                        else { System.Threading.Thread.Sleep(1500); }
                    }
                    catch (Exception ex)
                    {
                        LogMessageToFile("While Loop Validation Outcome is 'FALSE'");
                        LogMessageToFile("Exception found at Copying the test build is : " + ex.ToString() +Environment.NewLine+ "The Stack Trace for the Exception is : "+Environment.NewLine+ex.StackTrace.ToString());
                        throw new Exception(ex.ToString());
                        break;
                    }
                }

                output2 = "Completed";
                LogMessageToFile(output2);
                Console.WriteLine("Done");
            }
            catch (Exception ex)
            {
                LogMessageToFile(ex.ToString());
            }
            return ":" + output2;
        }

        public static long GetDirectorySize(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] subdirectories = Directory.GetDirectories(path);

            long size = files.Sum(x => new FileInfo(x).Length);
            foreach (string s in subdirectories)
                size += GetDirectorySize(s);
            return size;
        }

        public static string GetCmdOut(string target)
        {
            LogMessageToFile("Executing the command file : " + target);
            Console.WriteLine("Batch Command initiating...");
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = target;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
            string output1 = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            string output2 = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            LogMessageToFile(output1 + ":" + output2);
            LogMessageToFile("Successfully executed batch file command for : " + target);
            LogMessageToFile(output1+" : "+output2);
            return output1 + ":" + output2;
        }

        public static string GetMsTestPath()
        {
            LogMessageToFile("Initiated search for MSTest.exe Path...");
            string[] cntrlines = { "cd /",
                                 "dir MSTest.exe /s /p", 
                                 "exit"
                                 };
            string msTestPathBat = GetTempPath() + "msTestPath.bat";
            LogBatFileOutput(cntrlines, msTestPathBat);
            System.IO.File.WriteAllLines(msTestPathBat, cntrlines);
            return CommandUtils.GetCmdOut(msTestPathBat);
        }

        public static void DeleteTestBatFiles()
        {
            try
            {
                LogMessageToFile("Initiated force delete of test*.bat files ...");
                DirectoryInfo dInfo = new DirectoryInfo(ConfigurationManager.AppSettings["TestRunBatchFileLocation"]);
                FileInfo[] fInfo = dInfo.GetFiles("*.bat",SearchOption.TopDirectoryOnly);
                foreach(FileInfo fs in fInfo)
                {
                  fs.Delete();
                }
            }
            catch(Exception ex)
            {
                LogMessageToFile("The exception found while deleting the *.bat files from Automation folder is : "+ ex.ToString());
            }
        }

    }
}