using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Automation.Library
{
    public class ProductConfiguration
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static List<string> GetProductList()
        {
            var source = AssemblyDirectory + "\\ProductConfiguration.xml";
            XDocument xDoc = XDocument.Load(source);
            var products = xDoc.Descendants("Product").Elements().Select(e => e.Name);
            List<string> productList = products.Select(r => r.LocalName).ToList();
            productList.Sort();
            return productList;
        }

        public static Hashtable GetProductConfigurationHashtable(string productName)
        {
            Hashtable ht = new Hashtable();
            var source = AssemblyDirectory + "\\ProductConfiguration.xml";
            XDocument xDoc = XDocument.Load(source);

            var products = xDoc.Descendants(productName).Elements("Test").Select(e => e);
            foreach (var item in products)
            {
                ht.Add("TestDlls", item.FirstAttribute.Value);
            }
            products = xDoc.Descendants(productName).Elements("Build").Select(e => e);
            foreach (var item in products)
            {
                ht.Add("BuildDefinition", item.FirstAttribute.Value);
            }
            var type = xDoc.Descendants(productName).Elements().Select(e => e);
            List<string> temp = new List<string>();
            foreach (var item in type)
            {
                if (item.FirstAttribute.Value.ToUpper() == "true".ToUpper())
                {
                    temp.Add(item.Name.ToString());
                }
            }
            ht.Add("Type", string.Join(",", temp));
            return ht;
        }

        public static double GetProductRoiFactor(string productName)
        {
            var source = AssemblyDirectory + "\\ProductConfiguration.xml";
            XDocument xDoc = XDocument.Load(source);
            double roiFactorDecimal = 0;
            var products = xDoc.Descendants(productName).Elements("Roi").Select(e => e);
            foreach (var item in products)
            {
                roiFactorDecimal = Convert.ToDouble(item.FirstAttribute.Value);
            }
            return roiFactorDecimal;
        }

        public static string GetProductCentralUrl(string productName)
        {
            var source = AssemblyDirectory + "\\ProductConfiguration.xml";
            XDocument xDoc = XDocument.Load(source);
            string centralRunUrl = null;
            var products = xDoc.Descendants(productName).Elements("SplunkResults").Select(e => e);
            foreach (var item in products)
            {
                centralRunUrl = item.FirstAttribute.Value;
            }
            return centralRunUrl;
        }
    }
}
