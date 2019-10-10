using MultiSpeak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;


namespace Uppco.Multispeak.Interfaces
{
    [ServiceContract]
    public interface IMultiSpeakService
    {
        [OperationContract]
        void test();

        [OperationContract]
        string[] GetMethods();

        [OperationContract]
        [WebInvoke(UriTemplate = "ODEventNotification/outageDetectionEvent[]&{transactionID}", BodyStyle = WebMessageBodyStyle.Bare)]
        errorObject[] ODEventNotification(outageDetectionEvent[] ODEvents, string transactionID);

        errorObject[] InitiateOutageDetectionEventRequest(List<string> meterList, DateTime currentDate, string url, string methodName);
    }

    [DataContract]
    public class OutageDetectionClass
    {
        private List<string> meterList;
        private DateTime currentDate;
        private string url;
        private string methodName;

        [DataMember]
        public List<string> MeterList
        {
            get { return meterList; }
            set
            {
                if (meterList != value)
                {
                    meterList = value;
                }
            }
        }

        [DataMember]
        public DateTime CurrentDate
        {
            get { return currentDate; }
            set
            {
                if (currentDate != value)
                {
                    currentDate = value;
                }
            }
        }

        [DataMember]
        public string Url
        {
            get { return url; }
            set
            {
                if (url != value)
                {
                    url = value;
                }
            }
        }

        [DataMember]
        public string MethodName
        {
            get { return methodName; }
            set
            {
                if (methodName != value)
                {
                    methodName = value;
                }
            }
        }
    }
    
}
