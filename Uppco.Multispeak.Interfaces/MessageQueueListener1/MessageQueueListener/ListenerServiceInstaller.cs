using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using IBM.WMQ;
using System.Web.Hosting;
using System.Xml;
using System.Reflection;
//using MultiSpeak;
using log4net;

//using Uppco.Multispeak.Interfaces;



namespace Uppco.Multispeak.Interfaces
{
    public partial class Service1 : ServiceBase
    {
        //public Service1()
        //{
        //    InitializeComponent();
        //}

        //protected override void OnStart(string[] args)
        //{
        //}

        //protected override void OnStop()
        //{
        //}
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BackgroundWorker _listener = new BackgroundWorker();
        private static string _baseDir;
        public static MQQueueManager qManager = null;
        public static String hostName = "";
        public static String channel = "";
        public static String mqport = "";
        public static String strQueue = "";
        public static String manager = "";
        public static int pollIntervalTime;
        public static String userName = "";
        public static String password = "";
        public static String url = "";
        public static String mqUsername = "";
        public static String mqPassword = "";
        public static String server_url = "";

        public Service1()
        {
            readMQConfigParameters();
            InitializeComponent();
            _listener.WorkerSupportsCancellation = true;
            _listener.DoWork += new DoWorkEventHandler(WorkerMethod);
        }

        private void readMQConfigParameters()
        {
            XmlTextReader reader = null;
            var assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"MQConfig.xml");
            //var serverConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"Service_Config.xml");
            //if (!File.Exists(serverConfigPath))
            //{
            //    _log.ErrorFormat("File in path {0} does not exist.", serverConfigPath);
            //}
            //else
            //{
            //    _log.DebugFormat("Reading data from file available in path: {0}", serverConfigPath);
            //    reader = new XmlTextReader(serverConfigPath);
            //    while (reader.Read())
            //    {
            //        if (reader.NodeType == XmlNodeType.Element)
            //        {
            //            Get the name of the XML token

            //            switch (reader.Name)
            //            {
            //                case "Url":
            //                    server_url = reader.ReadString();
            //                    if (String.IsNullOrEmpty(server_url))
            //                    {
            //                        _log.ErrorFormat("Value for parameter {0} is missing in Service Config file.", reader.Name);
            //                    }
            //                    break;
            //            }
            //        }
            //    }
            //}
            if (!File.Exists(assemblyPath))
            {
                _log.ErrorFormat("File in path {0} does not exist.", assemblyPath);    
            }
            else
            {
                _log.DebugFormat("Reading data from file available in path: {0}", assemblyPath);
                reader = new XmlTextReader(assemblyPath);
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        //Get the name of the XML token
                        
                        switch (reader.Name)
                        {
                            case "MQServerName":
                                hostName = reader.ReadString();
                                if (String.IsNullOrEmpty(hostName))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name); 
                                }
                                break;
                            case "MQChannel":
                                channel = reader.ReadString();
                                if (String.IsNullOrEmpty(channel))
                                {                           
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name); 
                                }
                                break;
                            case "MQPort":
                                mqport = reader.ReadString();
                                if (String.IsNullOrEmpty(mqport))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name); 
                                }
                                break;
                            case "MQQueueMgr":
                                manager = reader.ReadString();
                                if (String.IsNullOrEmpty(manager))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name); 
                                }
                                break;
                            case "ResponseQueueName":
                                strQueue = reader.ReadString();
                                if (String.IsNullOrEmpty(strQueue))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name); 
                                }
                                break;
                            case "PollIntervalSeconds":
                                pollIntervalTime = Int32.Parse(reader.ReadString());
                                if (pollIntervalTime == 0)
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name); 
                                }
                                break;
                            case "Username":
                                userName = reader.ReadString();
                                if (String.IsNullOrEmpty(userName))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name);
                                }
                                break;
                            case "Password":
                                password = reader.ReadString();
                                if (String.IsNullOrEmpty(password))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name);
                                }
                                break;
                            case "Url":
                                url = reader.ReadString();
                                if (String.IsNullOrEmpty(url))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name);
                                }
                                break;
                            case "MQUsername":
                                mqUsername = reader.ReadString();
                                if (String.IsNullOrEmpty(mqUsername))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name);
                                }
                                break;
                            case "MQPassword":
                                mqPassword = reader.ReadString();
                                if (String.IsNullOrEmpty(mqPassword))
                                {
                                    _log.ErrorFormat("Value for parameter {0} is missing in Config file.", reader.Name);
                                }
                                break;
                        }
                    }
                }
            }
        }



        #region Service Methods

        protected override void OnStart(string[] args)
        {
            try
            {
                _baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
                _listener.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Exception in Service1::OnStart(). Message: {0}", ex.Message);
            }
        }
        protected override void OnStop()
        {
            if (_listener.WorkerSupportsCancellation == true)
            {
                _listener.CancelAsync();
                _log.Debug("Service1::OnStop() - Stopping Listener Service...");
                System.Threading.Thread.Sleep((pollIntervalTime + 1) * 1000);
            }
            _log.Debug("Service1::OnStop() - Listener Service stopped.");
        }

        #endregion Service Methods

        private  void WorkerMethod(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (!worker.CancellationPending)
            {
                using (MQHelper mq = new MQHelper())
                {
                    mq.username = mqUsername;
                    mq.password = mqPassword;

                    if (mq.Connect(hostName, manager, channel, mqport))
                    {
                         int pollingMS = pollIntervalTime * 1000;

                        bool hasMessage = false;
                        while (mq.IsConnected(manager))
                        {
                            hasMessage = mq.MessageWaiting(strQueue, pollingMS);
                            if (hasMessage)
                            {
                                string messageText = string.Empty;
                                while ((messageText = mq.GetMessage(strQueue)) != "")
                                {
                                    List<string> meterList = getMetersList(messageText);

                                    // Send meters list to InitiatePingMeterRequest
                                    InitiatePingMeterRequest(meterList);
                                }
                            }
                            if (worker.CancellationPending)
                                break;
                        }
                        
                    }
                    else
                    {
                        _log.Error("Could not connect to MQ, starting retry in one minute");
                        System.Threading.Thread.Sleep(60000);
                    }
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                    }
                            
                }
            }
        }

        private static List<String> getMetersList(string messageText)
        {
            List<string> metersList = null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(messageText);

            XmlNodeList endDeviceEventList = doc.SelectNodes("Message/Payload/EndDeviceEvents");
            if (endDeviceEventList != null && endDeviceEventList.Count > 0)
            {
                foreach (XmlNode endDeviceEventNode in endDeviceEventList)
                {
                    XmlNodeList mrIdNodes = endDeviceEventNode.SelectNodes("EndDeviceEvent/Assets/mRID");
                    if (mrIdNodes != null && mrIdNodes.Count > 0)
                    {
                        metersList = new List<string>();
                        foreach (XmlNode idNode in mrIdNodes)
                        {
                            string id = idNode.InnerText;
                           
                            metersList.Add(id);
                        }
                    }
                }
            }
            // add code here
            return metersList;
        }

        private void InitializeComponent()
        {
            // 
            // RequestListener
            // 
            _baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _listener.RunWorkerAsync();
            this.CanShutdown = true;

        }

        public static void InitiatePingMeterRequest(List<string> meterList)
        {
            try
            {
                if (meterList != null)
                {
                    if (meterList.Count > 0)
                    {
                        _log.Debug("Sending ping request...");
                        //Send meters to OD Web services to request ping action
                        //MultiSpeakMsgHeader multispeakHeader = new MultiSpeakMsgHeader();
                        //multispeakHeader.UserID = userName;
                        //multispeakHeader.Pwd = password;
                        
                        //MultiSpeakService multiSpeakService = new MultiSpeakService();
                        //multiSpeakService.MultiSpeakMsgHeaderValue = multispeakHeader;

                        //errorObject[] errors = new errorObject[0];

                        // //Send ping request
                        //errors = multiSpeakService.InitiateOutageDetectionEventRequest(meterList, System.DateTime.Now, server_url, "InitiateOutageDetectionEventRequest");
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed to initiate a request to ping meters. The error message : " + e.Message + " OD_Server_Client::InitiatePingMeterRequest");
            }
        }
    }
}
