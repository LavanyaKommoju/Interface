using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MultiSpeak;

namespace Uppco.Multispeak.Interfaces
{
    class MQMeterReader
    {
        public void InitiatePingMeterRequest(List<string> meterList, bool bWriteData, string sPath, bool windowsServiceCall, string days, bool ignoreError)
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

                        MultiSpeakService multispeak = new MultiSpeakService();
                         errorObject[] errorObj = new errorObject[0];
                         //multispeak.MultiSpeakMsgHeaderValue = msgHeader;
                         //errorObj = multispeak.InitiateOutageDetectionEventRequest(meterList.ToArray(), System.DateTime.Now, this.OA_Server_URL, "InitiateOutageDetectionEventRequest");
                         //multispeak.MultiSpeakMsgHeaderValue = msgHeader;
                        //errorObj = this.InitiateOutageDetectionEventRequest(meterList.ToArray(), System.DateTime.Now, this.OA_Server_URL, "InitiateOutageDetectionEventRequest");

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
