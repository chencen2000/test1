/**
   ___         __   __                      ______ _                          
  |_  |       / _| / _|                    |___  /| |                         
    | |  ___ | |_ | |_   ___  _ __  _   _     / / | |__    __ _  _ __    __ _ 
    | | / _ \|  _||  _| / _ \| '__|| | | |   / /  | '_ \  / _` || '_ \  / _` |
/\__/ /|  __/| |  | |  |  __/| |   | |_| | ./ /___| | | || (_| || | | || (_| |
\____/  \___||_|  |_|   \___||_|    \__, | \_____/|_| |_| \__,_||_| |_| \__, |
                                     __/ |                               __/ |
                                    |___/                               |___/ 
 **/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace iDeviceInfoSvcHost
{
    public class Device : IDevice
    {
        public System.IO.Stream enumDevice()
        {
            Stream ret = null;
            foreach(String ss in Util.ListDeviceInfo.Keys)
            {
                Console.WriteLine(ss);
            }
            string s = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(Util.ListDeviceInfo.Keys.ToArray());
            ret = new MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(s));
            return ret;
        }

        public System.IO.Stream getDeviceInformation(string id, string force, string type)
        {
            Stream ret = null;
            bool bforce = false;
            string s="";
            Boolean.TryParse(force, out bforce);
            if (string.Compare(type, "info", true) == 0) type = "";
            if (bforce || 
                !Util.ListDeviceInfo.ContainsKey(id) || 
                Util.ListDeviceInfo[id].Count == 0 || 
                (!string.IsNullOrEmpty(type)&&  !Util.ListDeviceInfo[id].ContainsKey("CarrierName")))
            {
                Program.logIt(string.Format("{0}   {1} {2}", id, force, type));
                Util.GetDeviceInfo(id, type);
            }
            else
            {
                if (!Util.ListDeviceInfo.ContainsKey(id) || !Util.ListDeviceInfo[id].ContainsKey("ActivationState"))
                {
                    Util.GetDeviceInfo(id, "");
                }
            }
            Mutex mutex = new Mutex(false, id);
            try
            {
                mutex.WaitOne();
                if (Util.ListDeviceInfo.ContainsKey(id))
                    s = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(Util.ListDeviceInfo[id]);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            Program.logIt("infojson:"+s);
            ret = new MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(s));
            return ret;
        }

        public System.IO.Stream getDeviceInfoWithKey(string id, string key, string domain)
        {
            Stream ret = null;
            Util.GetDeviceInfoWithkey(id, key, domain);
            String s = "";// (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(Util.ListDeviceInfo[id]);
            Mutex mutex = new Mutex(false, id);
            try
            {
                mutex.WaitOne();
                if (Util.ListDeviceInfo.ContainsKey(id))
                    s = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(Util.ListDeviceInfo[id]);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            Program.logIt(s);
            ret = new MemoryStream(System.Text.UTF8Encoding.Default.GetBytes(s));
            return ret;
        }
    }
}
