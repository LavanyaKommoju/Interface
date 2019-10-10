using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using log4net;
using MultiSpeak;

namespace Uppco.Multispeak.Interfaces
{
    /// <summary>
    /// Class that does most of the work for the interface. It accepts MultiSpeak style arguments and converts them
    /// to Websphere MQ messages for the OMS.
    /// </summary>
    public class MessageBroker
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MessageBroker));
        /// <summary>
        /// Creates MQ CreateCall messages from MutiSpeak OD events.
        /// </summary>
        /// <param name="ODEvents"></param>
        /// <param name="transactionID"></param>
        public static bool CreateLastGaspOutageCalls(outageDetectionEvent[] ODEvents, string transactionID)
        {
            _log.Debug("In CreateLastGaspOutageCalls() method");

            bool success = true;

            try
            {
                using (MQConnector mq = new MQConnector())
                {
                    if (mq.Connect("MqConnectionKey"))
                    {
                        using (CreateCallFactory messageFactory = new CreateCallFactory())
                        {
                            foreach (outageDetectionEvent ODEvent in ODEvents)
                            {
                                if ((ODEvent != null) && (ODEvent.outageEventType == outageEventType.Outage))
                                {
                                    _log.DebugFormat("Outage called for meterNo = {0} with outageEventType = {1}", ODEvent.outageLocation.meterNo, ODEvent.outageEventType);
                                    CreateCall msg = messageFactory.CreateCallMessage(transactionID, ODEvent.outageLocation.meterNo, ODEvent.outageLocation.accountNumber);
                                    if (msg != null)
                                    {                    
                                        string xmlString = msg.GetXml();
                                        _log.InfoFormat("CreateLastGaspOutageCalls()::CreateCall message created. Message: {0}", xmlString);
                                        if (mq.PutRequestMessage("MqConnectionKey", xmlString))
                                        {
                                            _log.Info("CreateLastGaspOutageCalls()::CreateCall message successfully pushed to MQ");
                                        }
                                        else
                                        {
                                            _log.Error("CreateLastGaspOutageCalls()::Failed to put CreateCall message in MQ");
                                        }
                                    }
                                    else
                                    {
                                        _log.Error("CreateLastGaspOutageCalls()::Failed to construct CreateCall message. So, cannot be pushed to MQ.");
                                        success = false;
                                    }
                                }
                                else if ((ODEvent != null) && (ODEvent.outageEventType == outageEventType.Restoration))
                                {
                                    _log.DebugFormat("Restoration called for meterNo = {0} with outageEventType = {1}", ODEvent.outageLocation.meterNo, ODEvent.outageEventType);
                                    messageFactory.processRestoreMessage(ODEvent.outageLocation.meterNo, transactionID, "");                           
                                }
                                else
                                {
                                    _log.DebugFormat("The request in neither Outage type nor Restoration type. Ignoring the request. Details: MeterNo = {0} with OutageEventType = {1}.", ODEvent.outageLocation.meterNo, ODEvent.outageEventType);
                                }
                            }
                        }
                    }
                    else
                    {
                        _log.Error("CreateLastGaspOutageCalls()::Error connecting to MQ");
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("An exception occured in CreateLastGaspOutageCalls(). Message: {0}", ex.Message);
                success = false;
            }
            return success;
        }
    }
}
