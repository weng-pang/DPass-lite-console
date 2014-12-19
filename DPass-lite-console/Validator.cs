using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AttLogs
{
    class Validator
    {
        // Validation Regex
        public const string IP_REGEX = @"\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
        public const string PORT_REGEX = @"\b\d{2}\b|\b\d{3}\b|\b\d{4}\b|\b\d{5}\b";

        private static Regex ipRegex = new Regex(IP_REGEX);
        private static Regex portRegex = new Regex(PORT_REGEX);

        public static bool checkIpAddress(string ipAddress)
        {
            Match ipMatch = ipRegex.Match(ipAddress.Trim());
            return ipMatch.Success;
        }

        public static bool checkPort(string port)
        {
            Match portMatch = portRegex.Match(port.Trim());
            return portMatch.Success;
        }

    }
}
