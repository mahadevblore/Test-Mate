using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Automation.Library
{
    public class ProductDetails
    {
        public static Dictionary<string, string> TestCaseMapping;

        public static List<string> GetProductList()
        {
            return ProductConfiguration.GetProductList();
        }

        public static List<string> GetBuildsList(string productName)
        {
            Hashtable productConfigurationHashtable = ProductConfiguration.GetProductConfigurationHashtable(productName);
            var buildLocation = productConfigurationHashtable["DropLocation"].ToString();
            if (Directory.Exists(buildLocation))
            {
                string[] list = Directory.GetDirectories(buildLocation);
                return list.ToList();
            }
            return null;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, true);
                }
            }
        }

        public static void CopyBuildToLocalMachine(string buildPath, object buildName)
        {
            CommandUtils.RBCopy(buildPath, ConfigurationManager.AppSettings["AppDataLocation"] + "\\TestBuilds\\" + buildName);
        }

        public static IEnumerable<string> GetDllsInBuild(string buildName)
        {
            var newList = new List<string>();
            var list = Directory.GetFiles(ConfigurationManager.AppSettings["AppDataLocation"] + "TestBuilds\\" + buildName);
            foreach (string file in list)
            {
                if (file.Contains(".dll"))
                {
                    var temp = file.Split('\\');
                    newList.Add(temp[temp.Length - 1]);
                }
            }
            return newList;
        }

        public static string[] GetCategoryList()
        {
            string[] categoryHashtable = { "P0", "P1" };
            return categoryHashtable;
        }

        public static Dictionary<string, List<string>> GetModuleTestCaseDictionary(string testBuildName, string testDllName, string categoryName)
        {
            Dictionary<string, List<string>> ModuleTestCaseDictionary = new Dictionary<string, List<string>>();
            TestCaseMapping = new Dictionary<string, string>();
            string[] assemPaths = Directory.GetFiles(ConfigurationManager.AppSettings["AppDataLocation"] + "TestBuilds\\" + testBuildName, "*.dll",SearchOption.AllDirectories);
            string assemPath = assemPaths.First(x => x.Contains(testDllName));
            Assembly assembly = Assembly.UnsafeLoadFrom(assemPath);

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray<Type>();

                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }
            }
            catch (Exception ex)
            {
                types = null;  
            }

            foreach (Type className in types)
            {
                var methods = className.GetMethods(BindingFlags.Public
                                                      | BindingFlags.Instance
                                                      | BindingFlags.InvokeMethod);

               var dt = methods.Select(x =>((MethodBase) x).CustomAttributes.Where(y=>y.AttributeType.FullName.Contains("TestCategoryAttribute")));
                foreach (MethodInfo methodInfo in methods)
                {
                    var tempHashtable = new Hashtable();
                    //Cast the methodinfo to methodbase to get all the attributes.
                    var a1 = ((MethodBase)(methodInfo));
                    //Get all custom attributes of the test method.
                    var a3 = a1.CustomAttributes;
                    //Get All Custom properties of the test case
                    foreach (CustomAttributeData customAttributeData in a3)
                    {
                        //Determine the type of test method attributes that are passed.
                        if (customAttributeData.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"
                            || customAttributeData.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.PriorityAttribute"
                            || customAttributeData.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"
                            || customAttributeData.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UITesting.PriorityAttribute")
                        {
                            var a4 = customAttributeData.ConstructorArguments;
                            var customAttributeTypedArguments = a4 as System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>;
                            if (customAttributeTypedArguments != null)
                            {
                                var a = customAttributeTypedArguments[0];
                                if (!tempHashtable.ContainsKey(customAttributeData.AttributeType.FullName))
                                {
                                    tempHashtable.Add(customAttributeData.AttributeType.FullName, a.Value);
                                }
                            }
                        }
                    }

                    if (categoryName.ToUpper() == "p1".ToUpper())
                    {
                        if (tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UnitTesting.PriorityAttribute")
                            && tempHashtable["Microsoft.VisualStudio.TestTools.UnitTesting.PriorityAttribute"].ToString() == "1"
                            && tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"))
                        {
                            if (!ModuleTestCaseDictionary.ContainsKey(tempHashtable["Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"].ToString()))
                            {
                                var myValues = new List<string> { methodInfo.Name };
                                ModuleTestCaseDictionary.Add(tempHashtable["Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"].ToString(), myValues);
                            }
                            else
                            {
                                var templist =
                                    ModuleTestCaseDictionary[
                                        tempHashtable[
                                            "Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"]
                                            .ToString()];
                                templist.Add(methodInfo.Name);

                                ModuleTestCaseDictionary[tempHashtable[
                                    "Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"]
                                    .ToString()] = templist;
                            }
                        }
                        else if (tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UITesting.PriorityAttribute")
                            && tempHashtable["Microsoft.VisualStudio.TestTools.UITesting.PriorityAttribute"].ToString() == "1"
                            && tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"))
                        {
                            if (!ModuleTestCaseDictionary.ContainsKey(tempHashtable["Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"].ToString()))
                            {
                                var myValues = new List<string> { methodInfo.Name };
                                ModuleTestCaseDictionary.Add(tempHashtable["Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"].ToString(), myValues);
                            }
                            else
                            {
                                var templist =
                                    ModuleTestCaseDictionary[
                                        tempHashtable[
                                            "Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"]
                                            .ToString()];
                                templist.Add(methodInfo.Name);

                                ModuleTestCaseDictionary[tempHashtable[
                                    "Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"]
                                    .ToString()] = templist;
                            }
                        }
                    }
                    else if (categoryName.ToUpper() == "p0".ToUpper())
                    {
                        if (tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UnitTesting.PriorityAttribute")
                               && tempHashtable["Microsoft.VisualStudio.TestTools.UnitTesting.PriorityAttribute"].ToString() == "0"
                              )
                        {
                            if (!ModuleTestCaseDictionary.ContainsKey(tempHashtable["Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"].ToString()))
                            {
                                var myValues = new List<string> { methodInfo.Name };
                                ModuleTestCaseDictionary.Add(tempHashtable["Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"].ToString(), myValues);
                            }
                            else
                            {
                                var templist =
                                    ModuleTestCaseDictionary[
                                        tempHashtable[
                                            "Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"]
                                            .ToString()];
                                templist.Add(methodInfo.Name);

                                ModuleTestCaseDictionary[tempHashtable[
                                    "Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute"]
                                    .ToString()] = templist;
                            }
                        }
                        else if (tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UITesting.PriorityAttribute")
                         && tempHashtable["Microsoft.VisualStudio.TestTools.UITesting.PriorityAttribute"].ToString() == "0"
                         && tempHashtable.ContainsKey("Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"))
                        {
                            if (!ModuleTestCaseDictionary.ContainsKey(tempHashtable["Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"].ToString()))
                            {
                                var myValues = new List<string> { methodInfo.Name };
                                ModuleTestCaseDictionary.Add(tempHashtable["Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"].ToString(), myValues);
                            }
                            else
                            {
                                var templist =
                                    ModuleTestCaseDictionary[
                                        tempHashtable[
                                            "Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"]
                                            .ToString()];
                                templist.Add(methodInfo.Name);

                                ModuleTestCaseDictionary[tempHashtable[
                                    "Microsoft.VisualStudio.TestTools.UITesting.TestCategoryAttribute"]
                                    .ToString()] = templist;
                            }
                        }
                    }
                }
            }

            return ModuleTestCaseDictionary;
        }

        public static string GetAbsoluteTestName(string testName, string dllLocation)
        {
            Assembly assembly = Assembly.UnsafeLoadFrom(dllLocation);
            string testcaseFullName = null;
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray<Type>();
            }

            foreach (Type className in types)
            {
                var methods = className.GetMethods(BindingFlags.Public
                                                      | BindingFlags.Instance
                                                      | BindingFlags.InvokeMethod);


                try
                {
                    testcaseFullName = className + "." + methods.Single(x => x.Name == testName).Name;
                    if (testcaseFullName != null)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                { LoggerUtil.LogMessageToFile(ex.ToString()); testcaseFullName = ""; }
            }

            return testcaseFullName;
        }

    }
}
