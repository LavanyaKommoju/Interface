using log4net;
using MultiSpeak;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace Uppco.Multispeak.Interfaces
{
    public class MultiSpeakService : IMultiSpeakService
    {
        //Lavanya added public modifier
        //MultiSpeakMsgHeader MultiSpeakMsgHeaderValue
        public MultiSpeakMsgHeader MultiSpeakMsgHeaderValue
        {
            get;
            set;
        }
        
        private static errorObject[] _noError = new errorObject[0];
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private static readonly ILog _log = LogManager.GetLogger("MultispeakService");

        [System.Web.Services.Protocols.SoapHeaderAttribute("MultiSpeakMsgHeaderValue", Direction = System.Web.Services.Protocols.SoapHeaderDirection.InOut)]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.multispeak.org/Version_4.0/GetMethods", RequestNamespace = "http://www.multispeak.org/Version_4.0", ResponseNamespace = "http://www.multispeak.org/Version_4.0", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string[] GetMethods()
        {
            return new string[] { "GetMethods", "ODEventNotification" };
        }

        [System.Web.Services.Protocols.SoapHeaderAttribute("MultiSpeakMsgHeaderValue", Direction = System.Web.Services.Protocols.SoapHeaderDirection.InOut)]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.multispeak.org/Version_4.0/ODEventNotification", RequestNamespace = "http://www.multispeak.org/Version_4.0", ResponseNamespace = "http://www.multispeak.org/Version_4.0", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public errorObject[] ODEventNotification(outageDetectionEvent[] ODEvents, string transactionID)
        {
            _log.Debug("In MultiSpeakService::ODEventNotification() method");
            try
            {
                //LogSOAPRequest();

                // Check for any outages first (i.e., event type is Outage or Restoration). Leave early if there are none.
                if (ODEvents != null)
                {
                    if (!EventsContainOutages(ODEvents))
                    {
                        _log.Info("No outage events in message, ignoring request.");
                    }
                    else
                    {
                        if (MessageBroker.CreateLastGaspOutageCalls(ODEvents, transactionID))
                            _log.Info("Message processed successfully");
                        else
                            _log.Info("Errors occurred processing message");
                    }
                }
                else
                {
                    _log.Info("ODEvents object is null, ignoring request.");
                }
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Exception caught in MultiSpeakService::ODEventNotification() for transactionID = {0} - {1}", transactionID, e.Message);
                return ErrorArrayFromException(e);
            }
            return _noError;
        }

        [System.Web.Services.Protocols.SoapHeaderAttribute("MultiSpeakMsgHeaderValue", Direction = System.Web.Services.Protocols.SoapHeaderDirection.InOut)]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.multispeak.org/Version_4.0/InitiateOutageDetectionEventRequest", RequestNamespace = "http://www.multispeak.org/Version_4.0", ResponseNamespace = "http://www.multispeak.org/Version_4.0", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]

        public errorObject[] InitiateOutageDetectionEventRequest(List<string> meterList, DateTime currentDate, string url, string methodName)
        {

            try
            {
                errorObject theError = new errorObject();
                theError.errorString = "test";
                theError.eventTime = DateTime.Now;
                return new errorObject[] { theError };
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Exception caught in InitiateOutageDetectionEventRequest(). Message: {0}", e.Message);
            }
            return _noError;
        }

        public void test()
        {
            // Test stub
            _log.Info("No outage events in message, ignoring request.");
            //List<outageDetectionEvent> outageEvents = new List<outageDetectionEvent>();
            //outageDetectionEvent event1 = new outageDetectionEvent();
            //outageLocation loc = new outageLocation();
            //loc.meterNo = "21016465";

            //event1.outageEventType = outageEventType.Outage;
            //event1.outageLocation = loc;
            List<outageDetectionEvent> outageEvents = new List<outageDetectionEvent>();
            outageDetectionEvent event1 = new outageDetectionEvent();
            outageLocation loc = new outageLocation();
            loc.meterNo = "21004062";

            event1.outageEventType = outageEventType.Restoration;
            event1.outageLocation = loc;
            outageEvents.Add(event1);

            MessageBroker.CreateLastGaspOutageCalls(outageEvents.ToArray(), "1");
        }

      #region Private Methods
        private void LogSOAPRequest()
        {
            using (System.IO.Stream requestStream = HttpContext.Current.Request.InputStream)
            {
                requestStream.Position = 0;
                using (System.IO.StreamReader reader = new System.IO.StreamReader(requestStream, System.Text.Encoding.UTF8))
                {
                    _log.InfoFormat("SOAP request: {0}", reader.ReadToEnd());
                }
            }
        }
        private static bool EventsContainOutages(outageDetectionEvent[] ODEvents)
        {
            bool outage = false;
            
            foreach (outageDetectionEvent ODEvent in ODEvents)
            {
                if (ODEvent.outageEventType == outageEventType.Outage ||
                    ODEvent.outageEventType == outageEventType.Restoration)
                {
                    outage = true;
                    break;
                }
            }
   
            return outage;
        }

        private static errorObject[] ErrorArrayFromException(Exception ex)
        {
            errorObject theError = new errorObject();
            theError.errorString = ex.Message;
            theError.eventTime = DateTime.Now;
            return new errorObject[] { theError };
        }
        #endregion
    }
}
