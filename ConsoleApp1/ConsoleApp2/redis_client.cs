using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApp2
{
    class redis_client
    {
        public static void test()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            if (!db.KeyExists("mykey"))
            {
                db.StringSet("mykey", "init_value");
            }
            string value = db.StringGet("mykey");

            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            if (!db.KeyExists("test"))
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("key1", "string_value");
                data.Add("key2", 64);
                string s = jss.Serialize(data);
                db.StringSet("test", s);
            }
            value = db.StringGet("test");
            Dictionary<string, object> d = jss.Deserialize<Dictionary<string, object>>(value);

            using (var sw = new StringWriter())
            {
                using (var xw = XmlWriter.Create(sw))
                {
                    // Build Xml with xw.
                    xw.WriteStartDocument();
                    xw.WriteStartElement("root");
                    xw.WriteStartElement("key1");
                    xw.WriteElementString("key", "value");
                    xw.WriteEndElement();
                    xw.WriteEndDocument();
                    xw.Flush();
                    db.StringSet("test.xml", sw.ToString());
                }
            }
            value = db.StringGet("test.xml");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);
        }
    }
}
