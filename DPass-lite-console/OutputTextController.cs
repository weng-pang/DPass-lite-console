using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace DPass
{
    public class OutputTextController
    {

        public OutputTextController()
        {

        }

        public static void write(string text){
            
            string outputText = getTimeSet() + text;
            Console.WriteLine(outputText);
            //http://msdn.microsoft.com/en-us/library/system.environment.newline%28v=vs.110%29.aspx
            File.AppendAllText("log.log", outputText + Environment.NewLine);

        }

        public static string getTimeSet()
        {
            // http://msdn.microsoft.com/en-us/library/zdtaw1bw%28v=vs.110%29.aspx
            DateTime currentTime = DateTime.Now;
            return "[" + currentTime.ToString("yyyy-MM-dd HH.mm.ss.ffff") + "]";
        }
    }
}
