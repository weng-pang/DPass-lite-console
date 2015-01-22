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
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json; // Load Reference hinted by http://stackoverflow.com/questions/2682147/where-is-the-system-runtime-serialization-json-namespace?lq=1

namespace AttLogs
{
    class Controller
    {
        private XmlSerializer xmlConfigurationSettings;
        private XmlSerializer xmlAttendanceRecordSet;
        private DataContractJsonSerializer jsonAttendanceSerializer =
    new DataContractJsonSerializer(typeof(List<AttLogs.AttendanceRecord>));
        private DataContractJsonSerializer jsonTransactionSerializer = new DataContractJsonSerializer(typeof(List<AttLogs.AttendanceResult>));
        private DataContractJsonSerializer jsonErrorSerializer = new DataContractJsonSerializer(typeof(AttLogs.ErrorResult));

        private FileStream attendanceXmlFile;
        private FileStream databaseAttendanceXmlFile;

        private Connector attendanceConnection = new Connector();
        private RestClient restClient;
        private RestRequest restClientRequest;

        private DPass.Host host;
        private DPass.Configurations configurations;
        private List<AttLogs.AttendanceRecord> attendanceRecordList;
        private List<AttLogs.AttendanceRecord> newAttendanceRecordList;
        private List<AttLogs.AttendanceRecord> databaseAttendanceRecordList;

        private static bool workCompleted = true;
        /**
         * Controller.cs
         * All data preparations and upload procedures are deployed here.
         * 
         * The process follows the order.
         * - Obtain all Configurations
         * - Limit Operation Time
         * - Connect to Attendance Device
         * - Upload to Remote Server
         * - Update local respository
         * 
         * 
         */
        public Controller(string configurationFileName)
        {
            
            // Read Configruation File
            try
            { 
                xmlConfigurationSettings = new XmlSerializer(typeof(DPass.Configurations));
                configurations = (DPass.Configurations)xmlConfigurationSettings.Deserialize(new FileStream(@configurationFileName + ".xml", FileMode.Open));
                 new OutputTextController(configurations);
            }
            catch (Exception e)
            {
                OutputTextController.write("Configuration File Error");
                OutputTextController.write(e.Message);
                applicationEnd();
            }
            // Arrange a thread for maximum operation time limit
            //http://stackoverflow.com/questions/3360555/how-to-pass-parameters-to-threadstart-method-in-thread
            Thread timeLimit = new Thread(() => executionCounter(configurations.maximumTime));
            timeLimit.Start();
            try
            {
                // read source host setting xml
                //https://www.udemy.com/blog/csharp-serialize-to-xml/
                xmlConfigurationSettings = new XmlSerializer(typeof(DPass.Host));
                host = (DPass.Host)xmlConfigurationSettings.Deserialize(new FileStream(@configurations.hostFile, FileMode.Open));
                if (!Validator.checkIpAddress(host.ipAddress) || !Validator.checkPort(Convert.ToString(host.port)))
                {
                    OutputTextController.write("ERROR: IP and Port cannot be null or incorrect");
                    applicationEnd();
                }
                // prepare storage name
                string databaseAttendanceXmlFileName = "archive/" + host.machineId + "." + OutputTextController.getTimeSet() + ".xml";
                databaseAttendanceXmlFile = new FileStream(databaseAttendanceXmlFileName, FileMode.Create);
            }
            catch (Exception e)
            {
                OutputTextController.write("HOST Setting File Error");
                //OutputTextController.write(e.Message);
                applicationEnd();
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
                OutputTextController.write("Record Repository is faulty or not presented. A new one is to be created");
                //OutputTextController.write(e.Message);
                //OutputTextController.write("ERROR IN" + e.StackTrace);
                attendanceRecordList = new List<AttendanceRecord>();
                //applicationEnd();
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
                        OutputTextController.write(f.Message);
                    }
                    else
                    {
                        OutputTextController.write("ERROR: No data from terminal returns!");
                    }
                    applicationEnd();
                }
                attendanceConnection.enableDevice(true);//re-enable the device
            // disregard current records
                databaseAttendanceRecordList = new List<AttendanceRecord>();
                OutputTextController.write("Downloaded Record Count:" + newAttendanceRecordList.Count + ",Current Record Count:" + attendanceRecordList.Count);
            // gauge for new attendance set
                bool newRecordSet = false;
                if (attendanceRecordList.Count == newAttendanceRecordList.Count)
                {// Possibly No New Records
                    OutputTextController.write("No New Records Found");
                }
                else if (attendanceRecordList.Count > newAttendanceRecordList.Count)
                {// Possibly Attendance Machine Updates
                    OutputTextController.write("Possible New Attendance Record Set Found");
                    //attendanceRecordList.AddRange(newAttendanceRecordList);
                    // database upload
                    databaseAttendanceRecordList = newAttendanceRecordList;
                    newRecordSet = true;
                }
                else if (attendanceRecordList.Count < newAttendanceRecordList.Count)
                {// New Records to replace current list
                    OutputTextController.write("New Records Found");
                    
                    // for new completely new database
                    if (attendanceRecordList.Count == 0)
                    {
                        databaseAttendanceRecordList = newAttendanceRecordList;
                    }
                    else
                    {
                        // database upload
                        for (int i = attendanceRecordList.Count; i < newAttendanceRecordList.Count; i++)
                        {// new record cursor shifts back by 1 // This part is deleted, previously attendanceRecordList.Count -1
                            databaseAttendanceRecordList.Add(newAttendanceRecordList[i]);
                        }
                    }
                    // apply the attendance list into final storage list
                    //attendanceRecordList = newAttendanceRecordList;
                }
                OutputTextController.write("Number of Records to be Uploaded:" + databaseAttendanceRecordList.Count);
            // upload new records to database
                //http://msdn.microsoft.com/en-us/library/system.convert.toboolean%28v=vs.110%29.aspx
                if (Convert.ToBoolean(databaseAttendanceRecordList.Count) || configurations.alwaysUpload) 
                {
                    
                    //http://stackoverflow.com/questions/78181/how-do-you-get-a-string-from-a-memorystream
                    MemoryStream jsonStream;
                    // perform batch fragmentation procedures here
                    List<AttendanceRecord> currentUploadSet = new List<AttendanceRecord>();
                    StreamReader jsonReader;
                    int uploadCount = 0;
                    string jsonOutput;
                    // reference for last item
                    // http://stackoverflow.com/questions/1068110/identifying-last-loop-when-using-for-each
                    AttendanceRecord lastRecord = null;
                    if (Convert.ToBoolean(databaseAttendanceRecordList.Count))
                    {
                        lastRecord = databaseAttendanceRecordList[databaseAttendanceRecordList.Count - 1];
                    }
                    foreach (AttendanceRecord currentRecord in databaseAttendanceRecordList)
                    {
                        currentUploadSet.Add(currentRecord);
                        // upload the record when reaching the end or batch limitation
                        if (currentUploadSet.Count == configurations.batchCount || currentRecord == lastRecord)
                        {
                            jsonStream = new MemoryStream();
                            jsonAttendanceSerializer.WriteObject(jsonStream, currentUploadSet);
                            jsonStream.Position = 0;
                            jsonReader = new StreamReader(jsonStream);
                            jsonOutput = jsonReader.ReadToEnd();
                            restClient = new RestClient(configurations.serverAddress);
                            restClientRequest = new RestRequest(configurations.normalRoute, Method.POST);
                            restClientRequest.AddParameter("key", configurations.accessKey);
                            restClientRequest.AddParameter("content", jsonOutput);
                            // execute the request
                            OutputTextController.write("Uploading to server:" + configurations.serverAddress +"TIME(S):" + ++uploadCount);
                            IRestResponse response = restClient.Execute(restClientRequest);

                            if (response.ErrorException != null)
                            {
                                OutputTextController.write(response.ErrorMessage);
                            }
                            else
                            {
                                var content = response.Content; // raw content as string

                                try
                                { // The memory stream is fed directly by the result set
                                    // http://philcurnow.wordpress.com/2013/12/29/serializing-and-deserializing-json-in-c/
                                    // if there is anything not conform with attendance result, an exception will be thrown.
                                    jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                                    jsonStream.Position = 0;
                                    List<AttLogs.AttendanceResult> transactionResults = (List<AttLogs.AttendanceResult>)jsonTransactionSerializer.ReadObject(jsonStream);
                                    if (transactionResults.Count == 0)
                                    {// additional check to ensure zero upload count is not caused by an error
                                        if (content != "[]") // this is a lazy way
                                        {
                                            throw new ExecutionEngineException(content);
                                        }
                                    }
                                    OutputTextController.write("Uploaded Record Count:" + transactionResults.Count + "TIME(S):" + uploadCount);
                                    string transactionList = "";
                                    int i = 0;
                                    foreach (var item in transactionResults)
                                    {
                                        transactionList += Convert.ToString(item.transactionId) + ";";
                                        // apply the transaction id to record
                                        databaseAttendanceRecordList[i].transactionId = item.transactionId;
                                        i++;
                                    }
                                    OutputTextController.write("Transaction Ids:" + transactionList);
                                    // apply the attendance list into final storage list
                                    attendanceRecordList.AddRange(currentUploadSet);
                                    // clear current upload set for next batch
                                    currentUploadSet.Clear();
                                }
                                catch (Exception e)
                                {
                                    OutputTextController.write("Upload is not done. It may be caused by failure from the server");
                                    //OutputTextController.write(e.Message);
                                    OutputTextController.write("Response From the server");
                                    OutputTextController.write(content);
                                    applicationEnd(); // place the rest of operation into halt
                                }
                            }
                        }
                    }
                 
                    

                }
                if (newRecordSet) // check for handling new set of records
                {
                    // transfer old record database to a new one
                    string oldDatabaseAttendanceXmlFileName = "archive/" + OutputTextController.getTimeSet() + "archive.xml";
                    FileStream oldDatabaseAttendanceXmlFile = new FileStream(oldDatabaseAttendanceXmlFileName, FileMode.Create);
                    xmlAttendanceRecordSet.Serialize(oldDatabaseAttendanceXmlFile, attendanceRecordList);
                    attendanceRecordList = databaseAttendanceRecordList; // this will completely replace the record
                }
                else
                {
                    // apply the attendance list into final storage list
                    //attendanceRecordList.AddRange(databaseAttendanceRecordList);
                }
                // update local record repository - only if update to remote database possible
                // https://www.udemy.com/blog/csharp-serialize-to-xml/
                attendanceXmlFile.SetLength(0); // Clear the original record repository
                xmlAttendanceRecordSet.Serialize(attendanceXmlFile, attendanceRecordList);
                xmlAttendanceRecordSet.Serialize(databaseAttendanceXmlFile, databaseAttendanceRecordList);
                workCompleted = true; // only when a full completed operation can mark work completed
            // Do not initialise rest client if no records to be added
                attendanceXmlFile.Close();
                databaseAttendanceXmlFile.Close();
                applicationEnd();
        }

        public static void executionCounter(int time)
        {
            Thread.Sleep(time);
            OutputTextController.write("Operation Time Exceeded. Application Force Exit. Maximum Time: "+ time +"ms");
            //http://stackoverflow.com/questions/12977924/how-to-properly-exit-a-c-sharp-application
            applicationExit(); // Just taking a normal exit
        }
        public static void applicationEnd()
        {
            // Prompt for application exit
            Console.WriteLine("Press Enter to Leave...");
            OutputTextController.write(Console.ReadLine());
            applicationExit(); 
        }
        public static void applicationExit()
        {
            if (workCompleted)
            {
                Environment.Exit(0); // Just taking a normal exit
            }
            Environment.Exit(1);
        }

    }


}
