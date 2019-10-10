using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Configuration;
using log4net;
using Intergraph.IPS.DB;

namespace Uppco.Multispeak.Interfaces
{
    /// <summary>
    /// Class to create outage messages for InService
    /// </summary>
    public class CreateCallFactory : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CreateCallFactory));
        private IPSConnection _connection = null;
        private IPSCommand _command = null;
        private bool _initialized = false;
        private bool _disposed = false;

        /// <summary>
        /// The constructor will call Initialize() to connect to the database
        /// and prepare a query used to add customer data to the messages it creates.
        /// </summary>
        public CreateCallFactory()
        {
            Initialize();
        }
        /// <summary>
        /// Connects to SQL and prepares a query used to retrieve customer data. 
        /// </summary>
        internal void Initialize()
        {
            _log.Debug("Connecting to DB...");
            _connection = new IPSConnection();
            int retryCount = 0;
            while (_connection.State != ConnectionState.Open)
            {
                try
                {
                    _connection.Open();
                }
                catch (Exception ex)
                {
                    _log.WarnFormat("Failed to connect to DB - {0}", ex.Message);
                    if (++retryCount >= 2) throw ex;
                    _log.WarnFormat("Retry attempt {0}", retryCount);
                    Random rand = new Random();
                    System.Threading.Thread.Sleep(rand.Next(500));
                }
            }

             _initialized = true;
        }

        /// <summary>
        /// Creates a CreateCall object and returns it. The CreateCall has all of the data from the 
        /// query results.
        /// </summary>
        /// <param name="transactionID"></param>
        /// <param name="meterNo"></param>
        /// <param name="accountNumber"></param>
        /// <returns>CreateCall message that can be placed in the MQ queue</returns>
        public CreateCall CreateCallMessage(string transactionID, string meterNo, string accountNumber)
        {
            if (!_initialized)
                Initialize();

            if (string.IsNullOrEmpty(meterNo))
            {
                _log.ErrorFormat("CreateCall message::meterNo is null or empty for transactionID: {0} and account number: {1}. Ignoring this request.", transactionID, accountNumber);
                return null;
            }

            CreateCall createCall = new CreateCall(transactionID, meterNo, accountNumber);

            string query = "select * from cispersl where meter_num = '" + meterNo +"'";
            _command = new IPSCommand(query, _connection);

            _log.DebugFormat("Reading cispersl table for meterNo '{0}'", meterNo);
            bool isSuccess = false;
            isSuccess = createCall.QueryForCustomerInfo(_command);

            if (!isSuccess)
            {
                _log.WarnFormat("No data found for meterNo '{0}'", meterNo);
                return null;
            }

            return createCall;
        }

        public void processRestoreMessage(string meterNumber, string id, string meterOffDts)
        {
            string query = "";
            string sName = "";
            string sPhone = "";
            string sLocation = "";
            string sXfmr = "";
            string sAccountNum = "";
            string sMeterNum = "";
            long lPremiseID = 0;
            int queryResult = 0;

            string sDate = String.Format("{0:yyyyMMddHHmmss}", System.DateTime.Now).ToString();

            try
            {
                IPSTransaction transaction = _connection.BeginTransaction();

                // query1
                query = "select name, phone, location, xfmr, account_num, meter_num, premise from cispersl where meter_num = '" + meterNumber + "'";
                _log.DebugFormat("processRestoreMessage()::query1 - {0}", query);
             
                using (IPSCommand cmd = new IPSCommand(query, _connection))
                {
                    using (IPSDataAdapter da = new IPSDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        int nrows = da.Fill(table);

                        foreach (DataRow row in table.Rows)
                        {

                            sName = row["name"].ToString();
                            sPhone = row["phone"].ToString();
                            sLocation = row["location"].ToString();
                            sXfmr = row["xfmr"].ToString();
                            sAccountNum = row["account_num"].ToString();
                            sMeterNum = row["meter_num"].ToString();
                            lPremiseID = Convert.ToInt64(row["premise"]);
                        }
                    }

                }// query1 end

                int iReqID = 0;
                int iID = 1;

                //query2
                query = "select n_num from numbr where purpose like 'AMI Unsolicited%' and curent='T'";
                _log.DebugFormat("processRestoreMessage()::query2 - {0}", query);
                 
                using (IPSCommand cmd = new IPSCommand(query, _connection))
                {
                    using (IPSDataAdapter da = new IPSDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        int nrows = da.Fill(table);

                        foreach (DataRow row in table.Rows)
                        {
                            if (row["n_num"] != System.DBNull.Value)
                            {
                                iID = Convert.ToInt32(row["n_num"]);
                            }
                            else
                            {
                                iID = iID + 1;
                            }

                        }
                    }

                } //query2 end

                String dst_value = String.Empty;
                String std_value = String.Empty;
                String is_dst_used = String.Empty;
                String suffix = String.Empty;

                //query3
                query = "select is_dst_used, dst_name, std_name from time_zone_info";
                _log.DebugFormat("processRestoreMessage()::query3 - {0}", query);

                using (IPSCommand cmd = new IPSCommand(query, _connection))
                {
                    using (IPSDataAdapter da = new IPSDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        int nrows = da.Fill(table);

                        foreach (DataRow row in table.Rows)
                        {

                            is_dst_used = row["is_dst_used"].ToString();
                            dst_value = row["dst_name"].ToString();
                            std_value = row["std_name"].ToString();
                        }
                    }
                }// query3 end

                if (is_dst_used != null)
                {
                    if (is_dst_used == "T")
                        suffix = dst_value;
                    else
                        suffix = std_value;
                }
                else
                {
                    suffix = dst_value;   //by default
                }

                //query4
                int xfmr_mslink = 0;
                if (!string.IsNullOrEmpty(sXfmr))
                {
                    query = "select mslink from OMS_TRANSFORMER where name = '"+ sXfmr + "'";
                    _log.DebugFormat("processRestoreMessage()::query4 - {0}", query);
                    
                    using (IPSCommand cmd = new IPSCommand(query, _connection))
                    {
                        using (IPSDataAdapter da = new IPSDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            int nrows = da.Fill(table);

                            foreach (DataRow row in table.Rows)
                            {
                                xfmr_mslink = Convert.ToInt32(row["mslink"]);
                            }
                        }
                    }// query4 end
                }           
                
                //query5
                query = "INSERT INTO AMI_DATA_RESPONSE_DETAILS (METER_NUM, METER_RESPONSE, ";
                query += "METER_RESPONSE_DTS, PROCESSING_COMPLETE, REQUEST_ID, UNSOLICITED_NOTIFICATION_ID, ";
                query += "XFMR_NAME, XFMR_MSLINK) VALUES ( '" + sMeterNum + "', 'Meter On', '" + sDate + suffix + "', 'T', " + iReqID + ", " + iID + ", '" + sXfmr + "' , " + xfmr_mslink + " )";
                
                _log.DebugFormat("processRestoreMessage()::query5 - {0}", query);
                
                using (IPSCommand cmd = new IPSCommand(query, _connection))
                {
                    queryResult = cmd.ExecuteNonQuery();
                } //query5 end

                //query6
                query = "update numbr set n_num = n_num + 1 where purpose like 'AMI Request%' and curent='T'";

                _log.DebugFormat("processRestoreMessage()::query6 - {0}", query);

                using (IPSCommand cmd = new IPSCommand(query, _connection))
                {
                    queryResult = cmd.ExecuteNonQuery();
                }
                //query6 end

                //query7
                query = "update numbr set n_num = n_num + 1 where purpose like 'AMI Unsolicited%' and curent='T'";

                _log.DebugFormat("processRestoreMessage()::query7 - {0}", query);

                using (IPSCommand cmd = new IPSCommand(query, _connection))
                {
                    queryResult = cmd.ExecuteNonQuery();
                }
                //query7 end

                transaction.Commit();
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Exception in processRestoreMessage() method. Message: {0}", e.Message);
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
                if (_connection != null)
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _log.Debug("Disconnecting from DB...");
                        _connection.Close();
                    }
                    _connection.Dispose();
                    _connection = null;
                }
                _disposed = true;
            }
        }
        #endregion
    }

    /// <summary>
    /// The class used to represent CreateCall messages
    /// </summary>
    public class CreateCall
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CreateCallFactory));
        private bool rowMatch = false;
        public bool RowMatch
        {
            get { return rowMatch; }
        }
        private string msgid = string.Empty;
        private string area = "01";
        private string fieldvalues = string.Empty;
        private string meterNo = string.Empty;
        private string accountNumber = string.Empty;
        private string name = string.Empty;
        private string location = string.Empty;
        private string phone = string.Empty;
        private string premise = string.Empty;
        private string dgroup = string.Empty;
        private string xfmr = string.Empty;
        private string meterStatus = string.Empty;
        private string custType = string.Empty;

        /// <summary>
        /// This constructor is not used directly, but must exist so the object can be serialized to xml.
        /// </summary>
        public CreateCall()
        {
        }
        /// <summary>
        /// The constructor called from the factory to create calls. The XmlAttribute tags are used to 
        /// format the object for MQ.
        /// </summary>
        /// <param name="transactionID"></param>
        /// <param name="meterNo"></param>
        /// <param name="accountNumber"></param>
        public CreateCall(string transactionID, string meterNo, string accountNumber)
        {
            if (!string.IsNullOrEmpty(transactionID))
                this.msgid = transactionID;
            else
                this.msgid = Guid.NewGuid().ToString();
            
            if (!string.IsNullOrEmpty(meterNo))
                this.meterNo = meterNo;
            
            if (!string.IsNullOrEmpty(accountNumber))
                this.accountNumber = accountNumber;
        }
        public string GetXml()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = true
                };

                using (XmlWriter writer = XmlWriter.Create(memoryStream, settings))
                {
                    writer.WriteStartElement("CreateCall");
                    writer.WriteAttributeString("msgid", this.msgid);
                    writer.WriteAttributeString("area", this.area);
                    writer.WriteAttributeString("fieldvalues", this.fieldvalues);
                    writer.WriteFullEndElement();
                    writer.Close();
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(memoryStream);
                string decodedString = reader.ReadToEnd();                
                reader.Close();
                return decodedString;
            }
        }

        /// <summary>
        /// Method to execute the query to add customer data to the object. The field names 
        /// read in this method must be included in the InitializeCommand() method of the factory class.
        /// </summary>
        /// <see cref="InitializeCommand()"/>
        /// <param name="cmd"></param>
        
        internal bool QueryForCustomerInfo(IPSCommand cmd)
        {
            bool isRow = false;
            try
            {  
                using (IPSDataAdapter da = new IPSDataAdapter(cmd))
                {           
                    DataTable table = new DataTable();
                    int nrows = da.Fill(table);

                    foreach (DataRow row in table.Rows)
                    {
                        this.name = Convert.ToString(row["NAME"]);
                        this.location = Convert.ToString(row["LOCATION"]);
                        if (row["PHONE"] == null)
                            this.phone = string.Empty;
                        else
                            this.phone = Convert.ToString(row["PHONE"]);

                        if (row["CUST_TYPE"] == null)
                            this.phone = string.Empty;
                        else
                            this.phone = Convert.ToString(row["CUST_TYPE"]);

                        this.premise = Convert.ToString(row["PREMISE"]);
                        this.dgroup = Convert.ToString(row["DGROUP"]);
                        this.accountNumber = Convert.ToString(row["ACCOUNT_NUM"]);
                        this.xfmr = Convert.ToString(row["XFMR"]);
                        this.custType = Convert.ToString(row["CUST_TYPE"]);
                        this.meterStatus = Convert.ToString(row["METER_STATUS"]);
                        this.meterNo = Convert.ToString(row["METER_NUM"]);
                    }
                    if (nrows >= 1)
                        isRow = true;
                }
                StringBuilder sb = new StringBuilder();
                sb.Append("CALLBACK=N;");
                sb.AppendFormat("NAME={0};", this.name);
                sb.AppendFormat("CUST_TYPE={0};", "A");  // InService wants 'A' in this field so it can render the calls on the map as an 'A' per Pratap 3/7/2013
                sb.AppendFormat("LOCATION={0};", this.location);
                sb.AppendFormat("PHONE={0};", this.phone);
                sb.Append("PRIORITY=;");
                sb.AppendFormat("PREMISE={0};", this.premise);
                sb.AppendFormat("METER={0};", this.meterNo);
                sb.Append("AGENCY=OUTAGE;");
                sb.AppendFormat("DGROUP={0};", this.dgroup);
                sb.Append("IS_METER=T;");
                sb.AppendFormat("ACCT={0};", this.accountNumber);
                sb.AppendFormat("XFMR={0}", this.xfmr);
                sb.Append("REMARKS=Call Created By AMI;");

                fieldvalues = sb.ToString();    
            }
            catch (IPSDataException o)
            {
                _log.ErrorFormat("Exception in QueryForCustomerInfo() method. Message: {0}", o.Message);
            }

            return isRow;
        }
        
    }
}
