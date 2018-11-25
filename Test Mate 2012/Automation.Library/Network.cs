using System;
using  System.Net;
using System.Net.Sockets;
namespace Automation.Library
{
    public class Network : LoggerUtil
    {
        public static bool GetResolvedConnecionIPAddress(string serverNameOrURL,
                           out IPAddress resolvedIPAddress)
        {
            LogMessageToFile("ServerName : " + serverNameOrURL);
            bool isResolved = false;
            IPHostEntry hostEntry = null;
            IPAddress resolvIP = null;
            try
            {
                if (!IPAddress.TryParse(serverNameOrURL, out resolvIP))
                {
                    LogMessageToFile("If loop to parse servername : "+serverNameOrURL);
                    try
                    {
                        hostEntry = Dns.GetHostEntry(serverNameOrURL);
                    }
                    catch (Exception ex)
                    {
                        LogMessageToFile("The exception caught at first attempt to resolve host is : "+ex.StackTrace.ToString()+"   "+ex.ToString());
                        serverNameOrURL = serverNameOrURL + "DOMAIN_NAME";
                        hostEntry = Dns.GetHostEntry(serverNameOrURL);
                    }
                    LogMessageToFile("The server name is : "+ serverNameOrURL);

                    if (hostEntry != null && hostEntry.AddressList != null
                                 && hostEntry.AddressList.Length > 0)
                    {
                        if (hostEntry.AddressList.Length == 1)
                        {
                            resolvIP = hostEntry.AddressList[0];
                            isResolved = true;
                        }
                        else
                        {
                            foreach (IPAddress var in hostEntry.AddressList)
                            {
                                if (var.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    resolvIP = var;
                                    isResolved = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    isResolved = true;
                }
            }
            catch (Exception ex)
            {
                LogMessageToFile("The exception caught while resolving Ip is : "+ex.ToString()+ "   " + ex.StackTrace.ToString());
                isResolved = false;
                resolvIP = null;
            }
            finally
            {
                LogMessageToFile("The resolved Ip is : " + resolvIP.ToString());

                resolvedIPAddress = resolvIP;
            }

            return isResolved;
        }
    }
}