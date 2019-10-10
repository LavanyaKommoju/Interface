using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Uppco.Multispeak.Interfaces
{
    [Serializable]
    public class MQConnection
    {
        [XmlAttribute]
        public string Key;
        [XmlAttribute]
        public string Host;
        [XmlAttribute]
        public string QueueManager;
        [XmlAttribute]
        public string Channel;
        [XmlAttribute]
        public string RequestQueue;
        [XmlAttribute]
        public string ResponseQueue;
        [XmlAttribute]
        public string Port;
        [XmlAttribute]
        public string Username;
        [XmlAttribute]
        public string Password;
    }

    [Serializable]
    public class Settings
    {
        public MQConnection[] MQSettings = new MQConnection[0];
    }
}
