using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace AttLogs
{ //http://stackoverflow.com/questions/14298968/is-there-a-way-to-alias-response-model-properties-in-asp-net-web-api
    [DataContract]
    [XmlRoot("AttendanceRecord")]
    public class AttendanceRecord
    {
        [DataMember(Name = "id")]
        [XmlElement(ElementName = "id")]
        public int sdwEnrollNumber {get; set;}

        private int idwVerifyMode = 0;

        public int IdwVerifyMode
        {
            get { return idwVerifyMode; }
            set { idwVerifyMode = value; }
        }
        //private int idwInOutMode = 0;

        [DataMember(Name = "entryId")]
        [XmlElement(ElementName = "entryId")]
        public int idwInOutMode {get; set;}

       private int idwYear = 0;

        public int IdwYear
        {
            get { return idwYear; }
            set { idwYear = value; }
        }
        private int idwMonth = 0;

        public int IdwMonth
        {
            get { return idwMonth; }
            set { idwMonth = value; }
        }
        private int idwDay = 0;

        public int IdwDay
        {
            get { return idwDay; }
            set { idwDay = value; }
        }
        private int idwHour = 0;

        public int IdwHour
        {
            get { return idwHour; }
            set { idwHour = value; }
        }
        private int idwMinute = 0;

        public int IdwMinute
        {
            get { return idwMinute; }
            set { idwMinute = value; }
        }
        private int idwSecond = 0;

        public int IdwSecond
        {
            get { return idwSecond; }
            set { idwSecond = value; }
        }
        private int idwWorkcode = 0;

        
        public int IdwWorkcode
        {
            get { return idwWorkcode; }
            set { idwWorkcode = value; }
        }

        // Specification for uploading data
        //private string ipAddress;
        [DataMember(Name = "ipAddress")]
        [XmlElement(ElementName = "ipAddress")]
        public string ipAddress {get; set;}

        //private int portNumber;
        [DataMember(Name = "portNumber")]
        [XmlElement(ElementName = "portNumber")]
        public int portNumber {get; set;}
        //{
        //    get { return portNumber; }
        //    set { portNumber = value; }
        //}

        //private int machineId;
        [DataMember(Name = "machineId")]
        [XmlElement(ElementName = "machineId")]
        public int machineId{get; set;}
        //{
        //    get { return machineId; }
        //    set { machineId = value; }
        //}

        //private string dateTime;
        [DataMember(Name = "dateTime")]
        [XmlElement(ElementName = "dateTime")]
        public string dateTime {get; set;}


        public void assembleDate()
        {
            this.dateTime = this.idwYear + "-" + this.idwMonth + "-" + this.idwDay + " " + this.idwHour + ":" + this.idwMinute;
        }
    }

    [DataContract]
    public class AttendanceResult
    {
        [DataMember(Name = "transactionId")]
        public int transactionId { get; set; }
    }
}
