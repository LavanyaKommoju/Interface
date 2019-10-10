using System;
using System.Collections;
using System.Text;
using IBM.WMQ;

namespace MQListener
{
    public class MQHelper : IDisposable
    {
        private MQQueueManager _queueManager = null;
        private bool _disposed = false;

        ////INSRV3
        //const String hostName = "INSRV3";
        //const String channel = "SYSTEM.DEF.SVRCONN";
        //const String strQueueManager = "QMINSERVICE";

        //*****KCPL Configuration******
        //const String hostName = "hpi01";
        //const String channel = "CLIENT.TO.GXPQM_D2";
        //const String strQueueManager = "GXPQM_D2";

        public String hostName = "";
        public String channel = "";
        public String port = "";
        public String strQueueManager = "";
        
        /// Connect to WebSphere using the configured settings
        /// </summary>
        /// <returns></returns>
        public bool Connect(string connectionKeyName)
        {
            bool connected = false;
            connected = Connect(hostName, strQueueManager, channel, port);
            return connected;
        }
                       
        public bool Connect(string connectionNameList, string queueManagerName, string channelName, string mqport)
        {
            try
            {
                Hashtable properties = new Hashtable();
                properties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);
                properties.Add(MQC.HOST_NAME_PROPERTY, connectionNameList);
                properties.Add(MQC.CHANNEL_PROPERTY, channelName);
                properties.Add(MQC.PORT_PROPERTY, mqport);
                //properties.Add(MQC.CONNECT_OPTIONS_PROPERTY, MQC.MQCNO_RECONNECT);
                properties.Add(MQC.USER_ID_PROPERTY, "nlkommoj");
                properties.Add(MQC.PASSWORD_PROPERTY, "KDhruvan#7702140404");
            
                _queueManager = new MQQueueManager(queueManagerName, properties);
                //eventLog1.WriteEntry("Connected to MQ");
                return _queueManager.IsConnected;
            }
            catch (MQException mqe)
            {
                //eventLog1.WriteEntry("");
                //eventLog1.WriteEntry("MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
                //eventLog1.WriteEntry(mqe.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                //eventLog1.WriteEntry("");
                //eventLog1.WriteEntry("Exception caught in MQHelper.Connect");
                //eventLog1.WriteEntry(ex);
                return false;
            }
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
                    //eventLog1.WriteEntry("MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
                }
            }
            catch (Exception ex)
            {
                //eventLog1.WriteEntry(ex);
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
                //eventLog1.WriteEntry("Accessing queue {0}", queueName);
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
                //eventLog1.WriteEntry("MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
                //throw new Exception(string.Format("Error writing to {0} queue", queueName));
            }
            finally
            {
                if (queue != null)
                {
                    //eventLog1.WriteEntry("Closing queue");
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
                //eventLog1.WriteEntry("Accessing queue {0}", queueName);
                queue = _queueManager.AccessQueue(queueName, MQC.MQOO_INPUT_AS_Q_DEF + MQC.MQOO_FAIL_IF_QUIESCING);

                //eventLog1.WriteEntry("Reading messages from queue");
                MQMessage message = new MQMessage();
                queue.Get(message);

                //eventLog1.WriteEntry("Message found in queue, reading content");
                messageText = message.ReadString(message.MessageLength);
                message.ClearMessage();
            }
            catch (MQException mqe)
            {
                if (mqe.ReasonCode == 2033)
                {
                    //eventLog1.WriteEntry("No messages in queue");
                    return "";
                }
                else
                {
                    //eventLog1.WriteEntry("MQException caught: {0} - {1}", mqe.ReasonCode, mqe.Message);
                    throw new Exception("Error reading message queue");
                }
            }
            finally
            {
                if (queue != null)
                {
                    //eventLog1.WriteEntry("Closing queue");
                    queue.Close();
                }
            }
            return messageText;
        }
        public static string GetTestMessage()
        {
            return "<CreateWoRequest><WOJobInfo><ag_id>ELECTRIC</ag_id> <dgroup>LUS</dgroup> <tycod>TREES</tycod> <typ_eng>Customer request TT</typ_eng> <num_1>T6741</num_1> <elus_cust_name>TERRELL J ALLEMAN</elus_cust_name> <elus_ami_enable>T</elus_ami_enable> <elus_meter_status>ACTIVE</elus_meter_status> <elus_phone_bus></elus_phone_bus> <elus_phone></elus_phone> <elus_phone_cell>(337) 322-4363</elus_phone_cell> <premise>12345754</premise> <account_num>6553985131</account_num> <elus_fdr_tap>7547</elus_fdr_tap> <elus_comments>Initial test of the InService to Cityworks integration</elus_comments> <elus_evnt_addr>501 ROBINHOOD CIR</elus_evnt_addr> <elus_cwtype>Tree Trim Customer</elus_cwtype> <elus_city>LAFAYETTE</elus_city> <elus_zip>70508</elus_zip> <elus_cust_addr>501 ROBINHOOD CIR</elus_cust_addr> <elus_fname>TERRELL J</elus_fname> <elus_lname>ALLEMAN</elus_lname> <elus_mi></elus_mi> <elus_initiatedby>658484</elus_initiatedby> <elus_xpers>658484</elus_xpers> <dev_name>345356</dev_name> <dev_type_name>FUSE</dev_type_name> <substation>7896</substation> <feeder>789601</feeder> </WOJobInfo></CreateWoRequest>";
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
