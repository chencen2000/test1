using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace iDeviceRealUDID
{
    class Program
    {
        static List<String> GetListDevices()
       {
           List<String> sDevice = new List<string>();
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://localhost:1930/device/enum");
            request.Method = "GET";
            String test = String.Empty;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    test = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine(e.StackTrace); 
            }
            if (!String.IsNullOrEmpty(test))
            {
                try
                {
                    var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                    sDevice = jss.Deserialize<List<String>>(test);
                }
                catch (Exception) { }
            }

            return sDevice;
        }
        static void Main(string[] args)
        {
            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, args);
            if (_args.Parameters.ContainsKey("udid"))
            {
                bool bFind = false;
                String suid = _args.Parameters["udid"];
                if (!String.IsNullOrEmpty(suid) && suid.Length == 40)
                {
                    Console.WriteLine(suid + "=" + suid);
                    return;
                }
                List<String> sdevs= GetListDevices();
                foreach (String s in sdevs)
                {
                    String ssss = suid;
                    String listudid = s;
                    //if(String.Compare(suid,s, true)==0)
                    //{
                    //    Console.WriteLine(suid+"="+s);
                    //    bFind = true;
                    //    break;
                    //}
                    //else 
                    if (String.Compare(ssss.Replace("-", ""), s.Replace("-", ""), true) == 0)
                    {
                        Console.WriteLine(suid + "=" + listudid);
                        bFind = true;
                        break;
                    }
                }
                if (!bFind)
                {//00008020-001974103C50003A
                    String studid = suid;
                    if (studid.Length == 24)
                    {
                        studid = studid.Insert(8, "-");
                    }
                    Console.WriteLine(suid + "=" + studid);
                }
            }
            else
            {
                Console.WriteLine("-udid=XXXX");
            }
        }
    }
}
