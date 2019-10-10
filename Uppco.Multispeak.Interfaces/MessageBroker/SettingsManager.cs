using System;
using System.IO;
using System.Xml.Serialization;
using System.Web.Hosting;

namespace Uppco.Multispeak.Interfaces
{
    public class SettingsManager
    {        
        public static object GetSettings(string xml, Type type)
        {
            FileInfo fi = new FileInfo(xml);
            if (fi.Exists)
            {
                XmlSerializer xs = new XmlSerializer(type);
                using (TextReader reader = new StreamReader(fi.FullName))
                {
                    return xs.Deserialize(reader);
                }
            }
            else
            {
                throw new Exception(fi.Name + " does not exist");
            }
        }
    }
}
