using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AttLogs
{
    public class Connector
    {
        //Create Standalone SDK class dynamically.
        private zkemkeeper.CZKEMClass ATTENDANCE;
        private bool connected = false;
        private static bool objectUsed = false;
        private string ipAddress;
        private int port;
        private int iMachineNumber = 1;//In fact,when you are using the tcp/ip communication,this parameter will be ignored,that is any integer will all right.Here we use 1.

       
        // Attendance Machine Status Code
        public static int COUNT_ALL_ATTENDANCE_ENTRY = 6;
            

        public Connector()
        {
            if (!objectUsed)
            {
                objectUsed = true;
            }
            else
            {
                //throw  new NotSupportedException("Only a single connection supported.");
            }
        }

        public bool isConnected()
        {  
            return connected;
        }

        public int IMachineNumber
        {
            get { return iMachineNumber; }
            set { iMachineNumber = value; }
        }

        public string getIpAddress()
        {
            return ipAddress;
        }

        public int getPort()
        {
            return port;
        }

        public zkemkeeper.CZKEMClass getAttendance()
        {
            return ATTENDANCE;
        }

        public bool connect(string ipAddress, string port)
        {
            try
            {
                this.ipAddress = ipAddress;
                this.port = Convert.ToInt16(port);
                // Connection procedure
                ATTENDANCE = new zkemkeeper.CZKEMClass();
                connected = ATTENDANCE.Connect_Net(ipAddress, Convert.ToInt32(port));
                if (connected)
                {
                    ATTENDANCE.RegEvent(iMachineNumber, 65535);//Here you can register the realtime events that you want to be triggered(the parameters 65535 means registering all)
                }
                return connected;
                // This particular part may spend a lot of time...
            }
            catch (COMException e)
            {
                Console.Write(e.ToString());
                return false;
            }
        }

        public void enableDevice(bool status)
        {
            ATTENDANCE.EnableDevice(iMachineNumber, status);//enable the device
        }

        public void disconnect()
        {
            ATTENDANCE.Disconnect();
            connected = false;
        }


        #region Data Operations
        public List<AttendanceRecord> readLogData()
        {
            // ready for parameters
            string sdwEnrollNumber = "";
            int idwVerifyMode=0;
            int idwInOutMode=0;
            int idwYear=0;
            int idwMonth=0;
            int idwDay=0;
            int idwHour=0;
            int idwMinute=0;
            int idwSecond = 0;
            int idwWorkcode = 0;
            // create an empty array list
            List<AttendanceRecord> attendanceRecordList = new List<AttendanceRecord>();

            bool successful = ATTENDANCE.ReadGeneralLogData(iMachineNumber);

            if (successful)
            {
                int recordCount = 0;
                while (ATTENDANCE.SSR_GetGeneralLogData(iMachineNumber, out sdwEnrollNumber, out idwVerifyMode,
                                out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))//get records from the memory
                {
                    AttendanceRecord aLogEntry = new AttendanceRecord();
                    recordCount++;
                    aLogEntry.IdwDay = idwDay;
                    aLogEntry.IdwHour = idwHour;
                    aLogEntry.idwInOutMode = idwInOutMode;
                    aLogEntry.IdwMinute = idwMinute;
                    aLogEntry.IdwMonth = idwMonth;
                    aLogEntry.IdwSecond = idwSecond;
                    aLogEntry.IdwVerifyMode = idwVerifyMode;
                    aLogEntry.IdwWorkcode = idwWorkcode;
                    aLogEntry.IdwYear = idwYear;
                    aLogEntry.sdwEnrollNumber = Convert.ToInt16(sdwEnrollNumber);
                    aLogEntry.assembleDate(); // Transform the date into proper format
                    aLogEntry.ipAddress = this.ipAddress;
                    aLogEntry.portNumber = this.port;
                    aLogEntry.machineId = this.iMachineNumber;
                    attendanceRecordList.Add(aLogEntry);
                    Console.WriteLine("Console:Processing Record: " + recordCount);
                }
            }
            else
            {
                throw new InvalidProgramException("Attendance Download Error");
            }
            return attendanceRecordList;
        }

        public int getDeviceStatus(int code)
        {
            int iValue = 0;
            bool successful = ATTENDANCE.GetDeviceStatus(iMachineNumber, code, ref iValue); //Here we use the function "GetDeviceStatus" to get the record's count.The parameter "Status" is 6.
            if (successful)
            {
                return iValue;
            }
            else
            {
                throw new InvalidProgramException("Attendance Status Error");
            }
        }

        public void deleteAllRecord()
        {
            bool successful = ATTENDANCE.ClearGLog(iMachineNumber);
            if (!successful)
            {
                throw new InvalidProgramException("Attendance Deletion Error");
            }
            else
            {
                ATTENDANCE.RefreshData(iMachineNumber);//the data in the device should be refreshed
            }
        }
        #endregion
    }
}
