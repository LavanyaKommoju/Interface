using System;
using System.IO;
using System.Web.Hosting;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Configuration;
using log4net;
using IBM.WMQ;

namespace Uppco.Multispeak.Interfaces
{
    /// <summary>
    /// Class to wrap the IBM MQ API to make it easier to get and put messages.
    /// </summary>
    // prakash
    //class MQConnector : IDisposable
    public class MQConnector : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MQConnector));
        private MQQueueManager _queueManager = null;
        private bool _disposed = false;
        private static Settings _settings;
        private MQConnection _activeConnection;
        private string username = string.Empty;
        private string password = string.Empty;

        
        public MQConnector()
        {
            FileInfo fi = new FileInfo(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "bin\\MQHelper.xml"));
            if (!fi.Exists)
                fi = new FileInfo(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "MQHelper.xml"));

            _settings = (Settings)SettingsManager.GetSettings(fi.FullName, typeof(Settings));
        }
        /// <summary>
        /// Connect to WebSphere using the configured settings
        /// </summary>
        /// <returns></returns>
        public bool Connect(string connectionKeyName)
        {
            _log.DebugFormat("MQConnector::Connect(string) - connectionKeyName:{0}", connectionKeyName);

            bool connected = false;            
            foreach (MQConnection config in _settings.MQSettings)
            {
                if (config.Key.Equals(connectionKeyName, StringComparison.CurrentCultureIgnoreCase))
                {
                    _log.DebugFormat("MQConnector::Connect(string)  - Attempting connection to queue manager {0} on {1}", config.QueueManager, config.Host);
                    username = config.Username;
                    password = config.Password;
                    connected = Connect(config.Host, config.QueueManager, config.Channel, config.Port);
                    if (connected)
                    {
                        _log.Debug("MQConnector::Connect(string)  - Connection succeeded");
                        this._activeConnection = config;
                        break;
                    }
                    else
                    {
                        _log.ErrorFormat("MQConnector::Connect(string) - Failed to MQ connect to queue manager {0} on {1}", config.QueueManager, config.Host);
                    }
                }
            }
            return connected;
        }
        /// <summary>
        /// Connect to WebSphere host, queue manager, and channel.
        /// </summary>
        /// <param name="connectionNameList"></param>
        /// <param name="queueManagerName"></param>
        /// <param name="channelName"></param>
        /// <returns></returns>
         private bool Connect(string hostName, string queueManagerName, string channelName, string port)
        {
            try
            {
                _log.DebugFormat("MQConnector::Connect() - hostName:{0}, queueManagerName:{1}, channelName:{2}, port:{3}", hostName, queueManagerName, channelName, port);

                Hashtable properties = new Hashtable();
                properties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);
                properties.Add(MQC.HOST_NAME_PROPERTY, hostName);
                properties.Add(MQC.CHANNEL_PROPERTY, channelName);
                properties.Add(MQC.PORT_PROPERTY, port);
                properties.Add(MQC.USER_ID_PROPERTY, username);
                properties.Add(MQC.PASSWORD_PROPERTY, password);

                _queueManager = new MQQueueManager(queueManagerName, properties);

                return _queueManager.IsConnected;
            }
            catch (MQException mqe)
            {
                _log.ErrorFormat("MQException in MQConnector::Connect(string, string, string, string) method. Message: {0}", mqe.Message);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Exception in MQConnector::Connect(string, string, string, string) method. Message: {0}", ex.Message);
            }

            return false;
        }
        /// <summary>
        /// Method to check if a connection to WebSphere exists and is valid.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected
        {
            get
            {
                if (_queueManager != null)
                    return _queueManager.IsConnected;
                else
                    return false;
            }
        }
        /// <summary>
        /// Puts a message string in the queue.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="messageString"></param>
        public bool PutRequestMessage(string connectionKeyName, string messageString)
        {
            bool success = true;
            try
            {
                MQMessage message = new MQMessage();
                message.CharacterSet = 1208;  // force to UTF-8 for InService
                message.WriteString(messageString);
                message.Format = MQC.MQFMT_STRING;
                
                PutRequestMessage(connectionKeyName, message);
            }
            catch (MQException mqe)
            {
                success = false;
                _log.ErrorFormat("Error writing to {0}. Error was {1}", _activeConnection.RequestQueue, mqe.Message);
                _log.Error(messageString);                
            }
            return success;
        }
        /// <summary>
        /// Puts a message object in the queue.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        private void PutRequestMessage(string connectionKeyName, MQMessage message)
        {
            MQQueue queue = null;

            try
            {
                if (!IsConnected)
                    Connect(connectionKeyName);

                MQPutMessageOptions queuePutMessageOptions = new MQPutMessageOptions();
                queue = _queueManager.AccessQueue(_activeConnection.RequestQueue, MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING);
                queue.Put(message, queuePutMessageOptions);
            }
            catch (MQException mqe)
            {
                _log.ErrorFormat("Exception in PutRequestMessage(). Message: {0}", mqe.Message);
            }
            finally
            {
                if (queue != null)
                {
                    queue.Close();
                }
            }
        }
                
        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                if ((_queueManager != null) && _queueManager.IsConnected)
                {
                    _log.Debug("Disconnecting from MQ");
                    _queueManager.Disconnect();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
