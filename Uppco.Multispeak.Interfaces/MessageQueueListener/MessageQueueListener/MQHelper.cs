using System;
using System.Collections;
using System.Text;
using log4net;
using IBM.WMQ;

namespace Uppco.Multispeak.Interfaces
{
    public class MQHelper : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MQHelper));
        private MQQueueManager _queueManager = null;
        private bool _disposed = false;
        
        public String hostName = "";
        public String channel = "";
        public String port = "";
        public String strQueueManager = "";
        public String username = "";
        public String password = "";

                
        /// Connect to WebSphere using the configured settings
        /// </summary>
        /// <returns></returns>
        public bool Connect(string connectionKeyName)
        {
            bool connected = false;
            connected = Connect(hostName, strQueueManager, channel, port);
            return connected;
        }
                       
       


        //public  bool Connect(string hostName, string queueManagerName, string channelName, string port)
        //{
        //    try
        //    {
        //        _log.DebugFormat("MQConnector::Connect() - hostName:{0}, queueManagerName:{1}, channelName:{2}, port:{3}", hostName, queueManagerName, channelName, port);

        //        Hashtable properties = new Hashtable();
        //        properties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);
        //        properties.Add(MQC.HOST_NAME_PROPERTY, hostName);
        //        properties.Add(MQC.CHANNEL_PROPERTY, channelName);
        //        properties.Add(MQC.PORT_PROPERTY, port);
        //        properties.Add(MQC.USER_ID_PROPERTY, username);
        //        properties.Add(MQC.PASSWORD_PROPERTY, password);

        //        _queueManager = new MQQueueManager(queueManagerName, properties);

        //        return _queueManager.IsConnected;
        //    }
        //    catch (MQException mqe)
        //    {
        //        _log.ErrorFormat("MQException in MQHelper::Connect(string, string, string, string) method. Message: {0}", mqe.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.ErrorFormat("Exception in MQHelper::Connect(string, string, string, string) method. Message: {0}", ex.Message);
        //    }

        //    return false;
        //}


        public bool Connect(string hostName, string queueManagerName, string channelName, string port)
        {
               try
                {
                    _log.DebugFormat("MQHelper::Connect() - hostName:{0}, queueManagerName:{1}, channelName:{2}, port:{3}", hostName, queueManagerName, channelName, port);

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
                    _log.ErrorFormat("MQException in MQHelper::Connect(string, string, string, string) method. Message: {0}", mqe.Message);
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Exception in MQHelper::Connect(string, string, string, string) method. Message: {0}", ex.Message);
                }
            
            return false;
        }
      
        
        
        
        
        public bool IsConnected(string queueManagerName)
        {
            if (_queueManager != null)
                return _queueManager.IsConnected;
            else
                return false;
        }
        public bool MessageWaiting(string queueName)
        {
            return MessageWaiting(queueName, 0);
        }
        public bool MessageWaiting(string queueName, int timeOutms)
        {
            bool hasMessage = false;
            MQQueue queue = null;
            MQMessage message = new MQMessage();

            try
            {
                // MQOO_BROWSE option means the message is NOT removed from the queue when it is read
                // We just want to know if there is at least one message in the queue so the listener
                // can take the appropriate action
                queue = _queueManager.AccessQueue(queueName, MQC.MQOO_BROWSE + MQC.MQOO_FAIL_IF_QUIESCING);

                MQGetMessageOptions opt = new MQGetMessageOptions();
                if (opt != null)
                {
                    opt.Options = IBM.WMQ.MQC.MQGMO_BROWSE_FIRST;
                    message.CorrelationId = IBM.WMQ.MQC.MQMI_NONE;
                    message.MessageId = IBM.WMQ.MQC.MQMI_NONE;

                    if (timeOutms > 0)
                    {
                        opt.Options += IBM.WMQ.MQC.MQGMO_WAIT;
                        opt.WaitInterval = timeOutms;
                    }
                }
                queue.Get(message, opt);
                hasMessage = true;
            }
            catch (MQException mqe)
            {
                if (mqe.ReasonCode == 2033)
                {
                    hasMessage = false;
                }
                else
                {
                    _log.ErrorFormat("MQHelper::MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Exception in MQHelper::MessageWaiting() method. Message: {0}", ex.Message);
            }
            finally
            {
                if (queue != null)
                {
                    queue.Close();
                }
            } 
            return hasMessage;
        }

        public bool PutMessage(string queueName, string message)
        {
            bool success = false;
            MQQueue queue = null;

            try
            {
                _log.DebugFormat("Accessing queue {0}", queueName);
                queue = _queueManager.AccessQueue(queueName, MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING);
                MQMessage queueMessage = new MQMessage();
                queueMessage.CharacterSet = 1208;  // force to UTF-8 for InService
                queueMessage.WriteString(message);
                queueMessage.Format = MQC.MQFMT_STRING;
                MQPutMessageOptions queuePutMessageOptions = new MQPutMessageOptions();
                queue.Put(queueMessage, queuePutMessageOptions);

                success=true;
            }
            catch (MQException mqe)
            {
                success = false;
                _log.ErrorFormat("MQHelper::MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
            }
            finally
            {
                if (queue != null)
                {
                    _log.Debug("MQHelper::PutMessage() - Closing queue...");
                    queue.Close();
                }
            }
            return success;

        }
        public string GetMessage(string queueName)
        {
            string messageText = string.Empty;
            MQQueue queue = null;

            try
            {
                queue = _queueManager.AccessQueue(queueName, MQC.MQOO_INPUT_AS_Q_DEF + MQC.MQOO_FAIL_IF_QUIESCING);

                MQMessage message = new MQMessage();
                queue.Get(message);

                messageText = message.ReadString(message.MessageLength);
                message.ClearMessage();
            }
            catch (MQException mqe)
            {
                if (mqe.ReasonCode == 2033)
                {
                    return "";
                }
                else
                {
                    _log.ErrorFormat("MQHelper::GetMessage() MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
                }
            }
            finally
            {
                if (queue != null)
                {
                    _log.Debug("MQHelper::GetMessage() - Closing queue...");
                    queue.Close();
                }
            }
            return messageText;
        }
        public static string GetTestMessage()
        {
            return "";
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
                    //eventLog1.WriteEntry("Disconnecting from MQ");
                    _queueManager.Disconnect();
                }
                
                _disposed = true;
            }
        }
        #endregion
    }
}
