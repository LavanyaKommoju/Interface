using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.ServiceProcess;
using System.ComponentModel;
using IBM.WMQ;
using System.Web.Hosting;
using System.Xml;
using System.Reflection;


namespace MQListener
{
    class MQMeterReader:ServiceBase
    {
        private BackgroundWorker _listener = new BackgroundWorker();
        private static string _baseDir;
        public static MQQueueManager qManager;
        public static String hostName = "";
        public static String channel = "";
        public static String mqport = "";
        public static String strQueue = "";
        public static String manager = "";
        public static int pollIntervalTime;

        public MQMeterReader()
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
            string projectPath = assemblyPath.Remove(assemblyPath.IndexOf("\\bin\\Debug"));
            //string path = Path.Combine(projectPath, "\\MQConfig.xml");
            string path = "C:\\Lavanya\\Interface\\Uppco.Multispeak.Interfaces\\MQListener\\MQConfig.xml";
           var fileName = "";
            if (!File.Exists(assemblyPath) && !File.Exists(path))
            {
                //fileName = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "MQConfig.xml");
                
            }
            //else if (!File.Exists(assemblyPath) &&  File.Exists(projectPath))
            //{
            //    string currentDirectory = Directory.GetCurrentDirectory();
            //    fileName = System.IO.Path.Combine(currentDirectory, "", "MQConfig.xml");
            //}
            else
            {
                //eventLog1.WriteEntry("AMI Config File: " + fileName);
                reader = new XmlTextReader(path);
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        //Get the name of the XML token
                        switch (reader.Name)
                        {
                            case "MQServerName":
                                hostName = reader.ReadString();
                                break;
                            case "MQChannel":
                                channel = reader.ReadString();
                                
                                break;
                            case "MQPort":
                                mqport = reader.ReadString();
                                break;
                            case "MQQueueMgr":
                                manager = reader.ReadString();
                                break;
                            case "ResponseQueueName":
                                strQueue = reader.ReadString();           
                                break;
                            case "PollIntervalSeconds":
                                pollIntervalTime = Int32.Parse(reader.ReadString());
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
                //_log.LogError("The service encountered an error during startup");
                //_log.LogException(ex);
            }
        }
        protected override void OnStop()
        {
            if (_listener.WorkerSupportsCancellation == true)
            {
                _listener.CancelAsync();
                //_log.LogInfo("Stopping Listener Service");
                System.Threading.Thread.Sleep((pollIntervalTime + 1) * 1000);
            }
            //_log.LogInfo("Listener Service stopped");
        }

        #endregion Service Methods

        private static void WorkerMethod(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (!worker.CancellationPending)
            {
                using (MQHelper mq = new MQHelper())
                {
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
                                //_log.LogInfo("AMI request detected in queue");
                                while ((messageText = mq.GetMessage(strQueue)) != "")
                                {
                                    //string messageFileName = SaveMessageToFile(messageText);
                                    //_log.LogDebug("Request saved in {0}", messageFileName);
                                    // read meter numbers from message and add them to list
                                    List<string> meterList = getMetersList(messageText);

                                    DirectoryInfo messageDirectory = new DirectoryInfo(Path.Combine(_baseDir, "..\\messages"));
                                    if (!messageDirectory.Exists)
                                        messageDirectory.Create();
                                    string spath = messageDirectory.ToString();

                                    // Send meters list to InitiatePingMeterRequest
                                    InitiatePingMeterRequest(meterList, true, spath, true, "30", true);

                                }
                            }
                            if (worker.CancellationPending)
                                break;
                        }
                        
                    }
                    else
                    {
                        //_log.LogError("Could not connect to MQ, starting retry in one minute");
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
            //DirectoryInfo messageDirectory = new DirectoryInfo(Path.Combine(_baseDir, "..\\messages"));
            //if (!messageDirectory.Exists)
            //    messageDirectory.Create();

            //string messageFileName = Path.Combine(messageDirectory.FullName, Guid.NewGuid().ToString() + ".xml");
            //TextWriter messageWriter = new StreamWriter(messageFileName);
            //messageWriter.Write(messageText);
            //messageWriter.Close();
            //return messageFileName;
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

        public static void InitiatePingMeterRequest(List<string> meterList, bool bWriteData, string sPath, bool windowsServiceCall, string days, bool ignoreError)
        {
            try
            {
                if (meterList != null)
                {
                    if (meterList.Count > 0)
                    {
                        // Check if user likes to write the data to a xml file.
                        if (bWriteData && sPath.Length > 0)
                        {
                            // Save the data to a xml file.
                            // Use serializer to write out to the file.
                            StringBuilder file = new StringBuilder(sPath);
                            if (sPath.LastIndexOf(@"\") != (sPath.Length - 1))
                                file.Append(@"\");
                            file.Append("InitiateOutageDetectionEventRequest_OD_Server_Client_");
                            file.Append(String.Format("{0:s}", DateTime.Now).Replace(":", "-"));
                            file.Append(".xml");

                            // Delete older files.
                            //UtilityMS30.DeleteXMLFiles(days, "InitiateOutageDetectionEventRequest_OD_Server_Client_", sPath);

                            XmlSerializer aSerializer = new XmlSerializer(typeof(string[]), "http://www.multispeak.org/Version_3.0");

                            TextWriter aWriter = new StreamWriter(file.ToString());

                            // Write each transformer bank.
                            aSerializer.Serialize(aWriter, meterList.ToArray());

                            aWriter.Close();
                        }

                        // Send meters to OD Web services to request ping action.
                        //MultiSpeakService.MultiSpeakMsgHeaderValue = msgHeader;
                        //errorObject[] errors = this.InitiateOutageDetectionEventRequest(meterList.ToArray(), System.DateTime.Now, this.OA_Server_URL, "InitiateOutageDetectionEventRequest");

                    }
                }
            }
            catch (Exception e)
            {
                //_log.Error("Failed to initiate a request to ping meters.  The error message : " + e.Message + " OD_Server_Client::InitiatePingMeterRequest");
                throw e;
            }
        }

    }
}
