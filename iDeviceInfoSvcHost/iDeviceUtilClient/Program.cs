using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace iDeviceUtilClient
{
    class Program
    {
        static public void logIt(string s)
        {
            System.Diagnostics.Trace.WriteLine(string.Format("[iDeviceUtilClient]: [{0}]: {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, s));
        }

        public static string[] runExe(string exeFilename, string param, out int exitCode, int timeout = 60*1000, string workingDir = "")
        {
            List<string> ret = new List<string>();
            exitCode = 1;
            Program.logIt(string.Format("[runExe]: ++ exe={0}, param={1}", exeFilename, param));
            try
            {
                if (System.IO.File.Exists(exeFilename))
                {
                    DateTime last_output = DateTime.Now;
                    DateTime _start = DateTime.Now;
                    System.Threading.ManualResetEvent ev = new System.Threading.ManualResetEvent(false);
                    Process p = new Process();
                    p.StartInfo.FileName = exeFilename;
                    p.StartInfo.Arguments = param;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    if (!string.IsNullOrEmpty(workingDir))
                        p.StartInfo.WorkingDirectory = workingDir;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    //p.EnableRaisingEvents = true;
                    p.OutputDataReceived += (obj, args) =>
                    {
                        last_output = DateTime.Now;
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Program.logIt(string.Format("[runExe]: {0}", args.Data));
                            ret.Add(args.Data);
                        }
                        if (args.Data == null)
                            ev.Set();
                    };
                    //p.Exited += (o, e) => { ev.Set(); };
                    p.Start();
                    _start = DateTime.Now;
                    p.BeginOutputReadLine();
                    bool process_terminated = false;
                    bool proces_stdout_cloded = false;
                    bool proces_has_killed = false;
                    while (!proces_stdout_cloded || !process_terminated)
                    {
                        if (p.HasExited)
                        {
                            // process is terminated
                            process_terminated = true;
                            Program.logIt(string.Format("[runExe]: process is going to terminate."));
                        }
                        if (ev.WaitOne(1000))
                        {
                            // stdout is colsed
                            proces_stdout_cloded = true;
                            Program.logIt(string.Format("[runExe]: stdout pipe is going to close."));
                        }
                        if ((DateTime.Now - last_output).TotalMilliseconds > timeout)
                        {
                            Program.logIt(string.Format("[runExe]: there are {0} milliseconds no response. timeout?", timeout));
                            // no output received within timeout milliseconds
                            if (!p.HasExited)
                            {
                                exitCode = 1460;
                                p.Kill();
                                proces_has_killed = true;
                                Program.logIt(string.Format("[runExe]: process is going to be killed due to timeout."));
                            }
                        }
                    }
                    if (!proces_has_killed)
                        exitCode = p.ExitCode;
                }
                else
                {
                    Program.logIt(string.Format("[runExe]: {0} not exist.", exeFilename));
                }
            }
            catch (Exception ex)
            {
                Program.logIt(string.Format("[runExe]: {0}", ex.Message));
                Program.logIt(string.Format("[runExe]: {0}", ex.StackTrace));
            }
            Program.logIt(string.Format("[runExe]: -- ret={0}", exitCode));
            return ret.ToArray();
        }

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
            }catch(Exception e)
            {
                logIt(e.Message);
                logIt(e.StackTrace);
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

        static void GetDeviceInfo(Uri uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
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
                }catch(Exception e)
                {
                    logIt(e.Message);
                    logIt(e.StackTrace);
                }

                if (!String.IsNullOrEmpty(test))
                {
                    try
                    {
                        var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                        Dictionary<String, String> sDevice = jss.Deserialize<Dictionary<String, String>>(test);
                        foreach (var sitem in sDevice)
                        {
                            Console.WriteLine(String.Format("{0}={1}", sitem.Key, String.IsNullOrEmpty(sitem.Value) ? "" : sitem.Value));
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception)
            {

            }

        }

        static String getRealUDID(String sUdid)
        {
            String s = sUdid;
            String aPath = System.AppDomain.CurrentDomain.BaseDirectory;
            String iDeviceRealUdid = Path.Combine(aPath, "iDeviceRealUDID.exe");
            if (File.Exists(iDeviceRealUdid))
            {
                int exitCode = 0;
                string[] ids = runExe(iDeviceRealUdid, string.Format("-udid={0}", sUdid), out  exitCode);
                foreach (String id in ids)
                {
                    if (id.StartsWith(sUdid + "="))
                    {
                        s = id.Replace(sUdid + "=", "");
                        break;
                    }
                }
            }

            return s;

        }

        static int Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            System.Configuration.Install.InstallContext _args = new System.Configuration.Install.InstallContext(null, args);
            if (_args.IsParameterTrue("list"))
            {
                List<String> listdev = GetListDevices();
                foreach (var s in listdev)
                {
                    Console.WriteLine(s);
                }

            }
            else if (_args.IsParameterTrue("info"))
            {//device/information?id={id}&force={force}&type={type}
                if (_args.Parameters.ContainsKey("udid"))
                {
                    String force = _args.IsParameterTrue("force").ToString();
                    String infobundle = _args.Parameters.ContainsKey("type") ? _args.Parameters["type"] : "info";
                    String id = getRealUDID(_args.Parameters["udid"]);

                    UriBuilder uri = new UriBuilder("http://localhost:1930/");
                    uri.Path = "device/information";
                    var collection = HttpUtility.ParseQueryString(string.Empty);
                    var query = new NameValueCollection
{ 
    {"id",id}, 
    { "type", infobundle },
    { "force", force },
}; 
                    foreach (var key in query.Cast<string>().Where(key => !string.IsNullOrEmpty(query[key])))
                    {
                        collection[key] = query[key];
                    }

                    uri.Query = collection.ToString();

                    GetDeviceInfo(uri.Uri);

                }
                else return 2;

            }
            else if (_args.IsParameterTrue("infokey"))
            {//device/info?id={id}&key={key}&domain={domain}
                if (_args.Parameters.ContainsKey("udid"))
                {
                    String key = _args.Parameters.ContainsKey("key") ? _args.Parameters["key"] : "";
                    String domain = _args.Parameters.ContainsKey("type") ? _args.Parameters["type"] : "";
                    String id = getRealUDID(_args.Parameters["udid"]);

                    UriBuilder uri = new UriBuilder("http://localhost:1930/");
                    uri.Path = "device/info";
                    var collection = HttpUtility.ParseQueryString(string.Empty);
                    var query = new NameValueCollection
{ 
    {"id",id}, 
    { "key", key },
    { "domain", domain },
};
                    foreach (var ky in query.Cast<string>().Where(ky => !string.IsNullOrEmpty(query[ky])))
                    {
                        collection[ky] = query[ky];
                    }

                    uri.Query = collection.ToString();

                    GetDeviceInfo(uri.Uri);
                }
                else return 1;
            }

            return 0;

        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logIt(e.ToString());
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;
            logIt(ex.StackTrace);
        }
    }
}
