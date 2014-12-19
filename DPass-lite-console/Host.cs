using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace DPass
{
    [XmlRoot("host")]
    public class Host
    {
        [XmlElement(ElementName = "ipaddress")]
        public string ipAddress;
        public int machineId;
        public int port;
        public string password;
    }

    [XmlRoot("server")]
    public class Server
    {
        public string address;
    }

    [XmlRoot("route")]
    public class Route
    {
        public string name;
        public string address;
    }
    [XmlRoot("configurations")]
    public class Configurations
    {
        public string serverAddress;
        public string normalRoute;
        public string batchRoute;
        public int batchCount;
        public string accessKey;
        public bool alwaysUpload;
        public string hostFile;
        public string recordFile;
        public int maximumTime;
    }
}
