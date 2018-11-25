using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Library
{
    public class Compatibility
    {
        public static string GetIEVersion()
        {
           RegistryKey regKey = Registry.LocalMachine;
           string key = regKey.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer").GetValue("svcVersion").ToString(); //For Windows 8 the key name is 'svcVersion'
           if (key == null)
           {
               key = regKey.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer").GetValue("Version").ToString();
           }
           return key;
        }

        public static string GetFirefoxVersion()
        {
            string wowNode = string.Empty;
            if (Environment.Is64BitOperatingSystem) wowNode = @"Wow6432Node\";
            RegistryKey regKey = Registry.LocalMachine;
            return regKey.OpenSubKey(@"Software\" + wowNode + @"Mozilla\Mozilla Firefox").GetValue("CurrentVersion").ToString();
        }

        public static string GetChromeVersion()
        {
            string key = null;
            string wowNode = string.Empty;
            if (Environment.Is64BitOperatingSystem) wowNode = @"Wow6432Node\";

            RegistryKey regKey = Registry.LocalMachine;
            RegistryKey keyPath = regKey.OpenSubKey(@"Software\" + wowNode + @"Google\Update\Clients");

            if (keyPath == null)
            {
                regKey = Registry.CurrentUser;
                keyPath = regKey.OpenSubKey(@"Software\" + wowNode + @"Google\Update\Clients");
            }

            if (keyPath == null)
            {
                regKey = Registry.LocalMachine;
                keyPath = regKey.OpenSubKey(@"Software\Google\Update\Clients");
            }

            if (keyPath == null)
            {
                regKey = Registry.CurrentUser;
                keyPath = regKey.OpenSubKey(@"Software\Google\Update\Clients");
            }

            if (keyPath != null)
            {
                string[] subKeys = keyPath.GetSubKeyNames();
                foreach (string subKey in subKeys)
                {
                    object value = keyPath.OpenSubKey(subKey).GetValue("name");
                    bool found = false;
                    if (value != null)
                        found =
                            value.ToString()
                                 .Equals("Google Chrome", StringComparison.InvariantCultureIgnoreCase);
                    if (found)
                    {
                        key= keyPath.OpenSubKey(subKey).GetValue("pv").ToString();
                    }
                }
            }
            return key;
        }

        public static string ComparisonLogic(string pName)
        {
            string msg = null;
            string chVersion = null;
            string ffVersion = null;
            string ieVersion = null;

            try
            {
                string ieCompatibility = null;
                string ffCompatibility = null;
                string chCompatibility = null;

                Dictionary<string, string> matrix = Query.GetCompatibilityMatrix(pName);

                if (matrix.Count() != 0)
                {
                    string ieReq = matrix["IE_Version"].Trim();
                    List<string> ieList = new List<string>();
                    if (ieReq.ToUpper() != "NA" || ieReq != null)
                    {
                        if (ieReq.Contains(','))
                        {
                            string[] strArr = ieReq.Split(',');
                            foreach (string str in strArr)
                            {
                                ieList.Add(str.Split('.')[0].Trim());
                            }
                        }
                    }

                    string ffReq = matrix["Firefox_Version"].Trim();
                    List<string> ffList = new List<string>();
                    if (ffReq.ToUpper() != "NA" || ffReq != null)
                    {
                        if (ffReq.Contains(','))
                        {
                            string[] strArr = ffReq.Split(',');
                            foreach (string str in strArr)
                            {
                                ffList.Add(str.Split('.')[0].Trim());
                            }
                        }
                    }

                    string chReq = matrix["Chrome_version"].Trim();
                    List<string> chList = new List<string>();
                    if (chReq.ToUpper() != "NA" || chReq != null)
                    {
                        if (chReq.Contains(','))
                        {
                            string[] strArr = chReq.Split(',');
                            foreach (string str in strArr)
                            {
                                chList.Add(str.Split('.')[0].Trim());
                            }
                        }
                    }

                    try
                    {
                        ieVersion = GetIEVersion().Split('.')[0];
                        //Comparison for IE
                        if (ieReq.Contains(','))
                        {
                            if (ieList.Where(x => x.Contains(ieVersion)).Count() >= 1)
                            {
                                ieCompatibility = "Compatible";
                            }
                            else
                            {
                                ieCompatibility = "May Not Be Compatible";
                            }
                        }
                        else if (ieReq.ToUpper() == "NA")
                        {
                            ieCompatibility = "May Not Be Compatible";
                        }
                        else
                        {
                            if (ieReq.Split('.')[0].Contains(ieVersion))
                            {
                                ieCompatibility = "Compatible";
                            }
                            else
                            {
                                ieCompatibility = "May Not Be Compatible";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerUtil.LogMessageToFile("Exception caught while comparing IE Version is : " + ex.ToString());
                        ffCompatibility = "Not Installed";
                    }

                    try
                    {
                        ffVersion = GetFirefoxVersion().Split('.')[0];
                        //Comparison for Firefox
                        if (ffReq.Contains(','))
                        {
                            if (ffList.Where(x => Convert.ToInt32(ffVersion) <= Convert.ToInt32(x.Split('.')[0])).Count() >= 1)
                            {
                                ffCompatibility = "Compatible";
                            }
                            else
                            {
                                ffCompatibility = "May Not Be Compatible";
                            }
                        }
                        else if (ffReq.ToUpper() == "NA")
                        {
                            ffCompatibility = "May Not Be Compatible";
                        }
                        else
                        {
                            if (Convert.ToInt32(ffVersion) <= Convert.ToInt32(ffReq.Split('.')[0]))
                            {
                                ffCompatibility = "Compatible";
                            }
                            else
                            {
                                ffCompatibility = "May Not Be Compatible";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerUtil.LogMessageToFile("Exception caught while comparing FF Version is : " + ex.ToString());
                        ffCompatibility = "Not Installed";
                    }


                    try
                    {
                        chVersion = GetChromeVersion().Split('.')[0];
                        //Comparison for Chrome
                        if (chReq.Contains(','))
                        {
                            if (chList.Where(x => Convert.ToInt32(chVersion) <= Convert.ToInt32(x.Split('.')[0])).Count() >= 1)
                            {
                                chCompatibility = "Compatible";
                            }
                            else
                            {
                                chCompatibility = "May Not Be Compatible";
                            }
                        }
                        else if (chReq.ToUpper() == "NA")
                        {
                            chCompatibility = "May Not Be Compatible";
                        }
                        else
                        {
                            if (Convert.ToInt32(chVersion) <= Convert.ToInt32(chReq.Split('.')[0]))
                            {
                                chCompatibility = "Compatible";
                            }
                            else
                            {
                                chCompatibility = "May Not Be Compatible";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerUtil.LogMessageToFile("Exception caught while comparing Chrome Version is : " + ex.ToString());
                        chCompatibility = "Not Installed";
                    }

                    msg = "IE - " + " Installed = " + ieVersion + " Required = " + ieReq + " Compatibility = " + ieCompatibility + "\r\n" +
                          "FF - " + " Installed = " + ffVersion + " Required = " + ffReq + " Compatibility = " + ffCompatibility + "\r\n" +
                          "CR - " + " Installed = " + chVersion + " Required = " + chReq + " Compatibility = " + chCompatibility + "\r\n" +
                          "Please Ensure the browsers are compatible for running Test Cases.";
                }
            }
            catch (Exception ex) { LoggerUtil.LogMessageToFile("Exception caught while computing compatibility is : "+ex.ToString()); }
            return msg;
        }
    }
}
