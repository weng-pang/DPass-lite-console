using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace DPass
{
    public class OutputTextController
    {
        private static DPass.Configurations configurations;
        // Combination of Constant and Static Variables not permitted
        public static string dateTimeFormat = "yyyy-MM-dd HH.mm.ss.ffff";
        public OutputTextController(DPass.Configurations newConfigurations)
        {
            configurations = newConfigurations;
        }

        public static void write(string text){
            
            string outputText = getTimeSet() + text;
            Console.WriteLine(outputText);
            //http://msdn.microsoft.com/en-us/library/system.environment.newline%28v=vs.110%29.aspx
            File.AppendAllText(@configurations.logFile, outputText + Environment.NewLine);

        }

        public static string getTimeSet()
        {
            // http://msdn.microsoft.com/en-us/library/zdtaw1bw%28v=vs.110%29.aspx
            DateTime currentTime = DateTime.Now;
            return "[" + currentTime.ToString(dateTimeFormat) + "]";
        }
    }
}
