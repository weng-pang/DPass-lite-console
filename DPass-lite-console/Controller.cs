using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using DPass;
using RestSharp;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json; // Load Reference hinted by http://stackoverflow.com/questions/2682147/where-is-the-system-runtime-serialization-json-namespace?lq=1

namespace AttLogs
{
    class Controller
    {
        private XmlSerializer xmlConfigurationSettings;
        private XmlSerializer xmlAttendanceRecordSet;
        DataContractJsonSerializer jsonSerializer =
    new DataContractJsonSerializer(typeof(List<AttLogs.AttendanceRecord>));

        private FileStream attendanceXmlFile;
        private FileStream databaseAttendanceXmlFile;

        private Connector attendanceConnection = new Connector();
        private RestClient restClient;
        private RestRequest restClientRequest;

        private DPass.Host host;
        private DPass.Server server;
        private DPass.Configurations configurations;
        private List<AttLogs.AttendanceRecord> attendanceRecordList;
        private List<AttLogs.AttendanceRecord> newAttendanceRecordList;
        private List<AttLogs.AttendanceRecord> databaseAttendanceRecordList;

        
        /**
         * 
         * All initial data preparations to be deployed here.
         * 
         * 
         * 
         * 
         * 
         */
        public Controller()
        {
            
            // Read Configruation File
            try
            { 
                xmlConfigurationSettings = new XmlSerializer(typeof(DPass.Configurations));
                configurations = (DPass.Configurations)xmlConfigurationSettings.Deserialize(new FileStream(@"configurations.xml", FileMode.Open));
            }
            catch (Exception e)
            {
                OutputTextController.write("Configuration File Error");
                OutputTextController.write(e.Message);
                return;
            }
            // Arrange a thread for maximum operation time limit
            //http://stackoverflow.com/questions/3360555/how-to-pass-parameters-to-threadstart-method-in-thread
            Thread timeLimit = new Thread(() => executionCounter(configurations.maximumTime));
            timeLimit.Start();
            // prepare storage name
            string databaseAttendanceXmlFileName = OutputTextController.getTimeSet() + ".xml";
            databaseAttendanceXmlFile = new FileStream(databaseAttendanceXmlFileName, FileMode.Create);


            try
            {
                // configurate destination file
                xmlConfigurationSettings = new XmlSerializer(typeof(DPass.Server));
                server = (DPass.Server)xmlConfigurationSettings.Deserialize(new FileStream(@"server.xml", FileMode.Open));
            }
            catch (Exception e)
            {
                OutputTextController.write("Server Setting File Error");
                OutputTextController.write(e.Message);
                return;
            }
            try
            {
                // read source host setting xml
                //https://www.udemy.com/blog/csharp-serialize-to-xml/
                xmlConfigurationSettings = new XmlSerializer(typeof(DPass.Host));
                host = (DPass.Host)xmlConfigurationSettings.Deserialize(new FileStream(@configurations.hostFile, FileMode.Open));
                if (!Validator.checkIpAddress(host.ipAddress) || !Validator.checkPort(Convert.ToString(host.port)))
                {
                    OutputTextController.write("ERROR: IP and Port cannot be null or incorrect");
                    return;
                }
            }
            catch (Exception e)
            {
                OutputTextController.write("HOST Setting File Error");
                //OutputTextController.write(e.Message);
                return;
            }
            try
            {
                // read current local record repository
                xmlAttendanceRecordSet = new XmlSerializer(typeof(List<AttLogs.AttendanceRecord>));
                attendanceXmlFile = new FileStream(configurations.recordFile, FileMode.OpenOrCreate);
                attendanceRecordList = (List<AttLogs.AttendanceRecord>)xmlAttendanceRecordSet.Deserialize(attendanceXmlFile);
            }
            catch (InvalidOperationException e)
            {
                OutputTextController.write("Record Repository is fault or not presented. A new one is to be created");
                //OutputTextController.write(e.Message);
                //OutputTextController.write("ERROR IN" + e.StackTrace);
                attendanceRecordList = new List<AttendanceRecord>();
                //return;
            }
            int idwErrorCode = 0;
            
                // connect to desginated device
                OutputTextController.write("Connecting to the site " + "IP ADDRESS:" + host.ipAddress + " PORT:" + host.port + " MACHINE ID:"+ host.machineId);

                attendanceConnection.connect(host.ipAddress, host.port.ToString());
                attendanceConnection.IMachineNumber = host.machineId;
                if (!attendanceConnection.isConnected())
                {
                    attendanceConnection.getAttendance().GetLastError(ref idwErrorCode);
                    OutputTextController.write("ERROR: Unable to connect the device,ErrorCode=" + idwErrorCode.ToString());
                }
                // download all records
                attendanceConnection.enableDevice(false);//disable the device
                try
                {
                    //read all the attendance records to the memory
                    // check for documentation about this area....
                    OutputTextController.write("Obtaining List from Attendance Device");
                    newAttendanceRecordList = attendanceConnection.readLogData();
                    OutputTextController.write("List Obtained Successfully");

                }
                catch (Exception f)
                {
                    attendanceConnection.getAttendance().GetLastError(ref idwErrorCode);

                    if (idwErrorCode != 0)
                    {
                        OutputTextController.write("ERROR: Reading data from terminal failed,ErrorCode: " + idwErrorCode.ToString());
                    }
                    else
                    {
                        OutputTextController.write("ERROR: No data from terminal returns!");
                    }
                    return;
                }
                attendanceConnection.enableDevice(true);//re-enable the device
            // disregard current records
                databaseAttendanceRecordList = new List<AttendanceRecord>();
                OutputTextController.write("Downloaded Record Count:" + newAttendanceRecordList.Count + ",Current Record Count:" + attendanceRecordList.Count);
                if (attendanceRecordList.Count == newAttendanceRecordList.Count)
                {// Possibly No New Records
                    OutputTextController.write("No New Records Found");
                }
                else if (attendanceRecordList.Count > newAttendanceRecordList.Count)
                {// Possibly Attendance Machine Updates
                    OutputTextController.write("Possible New Attendance Record Set Found");
                    attendanceRecordList.AddRange(newAttendanceRecordList);
                    // database upload
                    databaseAttendanceRecordList = newAttendanceRecordList;
                }
                else if (attendanceRecordList.Count < newAttendanceRecordList.Count)
                {// New Records to replace current list
                    OutputTextController.write("New Records Found");
                    attendanceRecordList = newAttendanceRecordList;
                    // database upload
                    for (int i = attendanceRecordList.Count - 1; i < newAttendanceRecordList.Count;i++ )
                    {// new record cursor shifts back by 1
                        databaseAttendanceRecordList.Add(newAttendanceRecordList[i]);
                    }
                }
                OutputTextController.write("Number of Records to be Uploaded:" + databaseAttendanceRecordList.Count);
            // upload new records to database
                //http://msdn.microsoft.com/en-us/library/system.convert.toboolean%28v=vs.110%29.aspx
                if (Convert.ToBoolean(databaseAttendanceRecordList.Count) || configurations.alwaysUpload) 
                {
                    OutputTextController.write("Uploading to server:" + configurations.serverAddress);
                    MemoryStream jsonStream = new MemoryStream();
                    jsonSerializer.WriteObject(jsonStream, databaseAttendanceRecordList); // TODO change gack to databaseAttendanceRecordList
                    jsonStream.Position = 0;
                    StreamReader jsonReader = new StreamReader(jsonStream);
                    string jsonOutput = jsonReader.ReadToEnd();
                    restClient = new RestClient(configurations.serverAddress);
                    restClientRequest = new RestRequest(configurations.normalRoute, Method.POST);
                    restClientRequest.AddParameter("key", configurations.accessKey);
                    // check for upload size
                    if (databaseAttendanceRecordList.Count > configurations.batchCount)
                    {// This part reserves for future implmentation of single or batch uploads
                        // TODO leave this part empty
                    }
                    OutputTextController.write(jsonOutput);
                    
                    restClientRequest.AddParameter("content", jsonOutput);

                    // execute the request
                    IRestResponse response = restClient.Execute(restClientRequest);
                    if (response.ErrorException != null)
                    {
                        OutputTextController.write(response.ErrorMessage);
                    }
                    else
                    {
                        var content = response.Content; // raw content as string
                        OutputTextController.write(content);
                        // update local record repository - only if update to remote database possible
                        // https://www.udemy.com/blog/csharp-serialize-to-xml/
                        attendanceXmlFile.SetLength(0); // Clear the original record repository
                        xmlAttendanceRecordSet.Serialize(attendanceXmlFile, attendanceRecordList);
                        xmlAttendanceRecordSet.Serialize(databaseAttendanceXmlFile, databaseAttendanceRecordList);
                        
                    }
                } // Do not initialise rest client if no records to be added
                attendanceXmlFile.Close();
                databaseAttendanceXmlFile.Close();
        }

        public static void executionCounter(int time)
        {
            Thread.Sleep(time);
            OutputTextController.write("Operation Time Exceeded. Application Exit. Maximum Time: "+ time +"ms");
            //http://stackoverflow.com/questions/12977924/how-to-properly-exit-a-c-sharp-application
            Environment.Exit(0); // Just taking a normal exit
        }


    }


}
