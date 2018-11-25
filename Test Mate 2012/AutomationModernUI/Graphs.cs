using System.Collections.Generic;

namespace AutomationModernUI
{
    public class Graphs
    {
        public static List<KeyValuePair<string, int>> ShowChart()
        {
            var myValue = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("Passed", 10),
                new KeyValuePair<string, int>("Failed", 5)
            };

            return myValue;
        }
    }
}
