using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Automation.Library
{
    public class OTGen : LoggerUtil
    {
        private static Guid GuidFromString(string data)
        {
            LogMessageToFile("Creating GUID String for : "+data);
            SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
            byte[] hash = provider.ComputeHash(System.Text.Encoding.Unicode.GetBytes(data));
            byte[] toGuid = new byte[16];
            Array.Copy(hash, toGuid, 16);
            LogMessageToFile("New Guid generated is : "+ new Guid(toGuid));
            return new Guid(toGuid);
        }

       public static string CreateOrderedTest(List<string> testcases, string dllName, string dllLocation,string namespaceURI,string typeOfMicrosoftVersion,string otLocation)
        {
            try
            {
                LogMessageToFile("Trying to create Ordered Test file");
                XNamespace xn = namespaceURI;
                LogMessageToFile("NameSpace URI is : " + namespaceURI);
                XDocument xDoc = new XDocument(new XDeclaration("1.0",
                                                        "utf-8", "yes"),
                    new XElement(xn + "OrderedTest",
                        new XAttribute("name", dllName.Split(new string[] { ".dll" }, StringSplitOptions.None)[0]),
                        new XAttribute("storage", dllLocation.Replace(".dll", ".orderedtest").ToLower()),
                        new XAttribute("id", GuidFromString(dllName.Split(new string[] { ".dll" }, StringSplitOptions.None)[0]).ToString()),
                        new XAttribute("continueAfterFailure", "true"),
                        new XAttribute("xmlns", namespaceURI)));
                LogMessageToFile("XDeclaration of Main Element.");
                XElement xEle = xDoc.Root;
                xEle.Add(new XElement(xn + "TestLinks"));
                foreach (string testcase in testcases)
                {
                    XElement connStrring = new XElement(xn + "TestLink",
                          new XAttribute("id", GuidFromString(ProductDetails.GetAbsoluteTestName(testcase, dllLocation)).ToString()),
                          new XAttribute("name", testcase),
                          new XAttribute("storage", dllName.ToLower()),
                          new XAttribute("type", typeOfMicrosoftVersion));
                    LogMessageToFile("Created Test Link : " + GuidFromString(ProductDetails.GetAbsoluteTestName(testcase, dllLocation)).ToString() + " : " +
                        testcase + " : " + dllName + " : " + typeOfMicrosoftVersion);
                    xDoc.Root.Element(xn + "TestLinks").Add(connStrring);
                    LogMessageToFile("Added Test Link : " + testcase);
                }

                LogMessageToFile("The Ordered Test Created is : " + xDoc.ToString());
                //string test = xDoc.+();
                string filePath = otLocation + "\\" + dllName.Split(new string[] { ".dll" }, StringSplitOptions.None)[0] + ".orderedtest";
                LogMessageToFile("File Location for the ordered test is : " + filePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LogMessageToFile("Deleted existing OT file.");
                }
                xDoc.Save(filePath);
                LogMessageToFile("Saved the OT file.");
                return filePath;
            }
            catch (Exception ex)
            {
                LogMessageToFile("Exception caught while creating OT file is : "+ex.ToString()+ " : " + ex.StackTrace);
                return "";
            }
        }

    }
}