using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace iDeviceInfoSvcHost
{
    public class Util
    {

        public static ConcurrentDictionary<String, ConcurrentDictionary<String, String>> ListDeviceInfo = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        public static Boolean bExit = false;
        public static Boolean IsMaxCapacity = false;
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        const string DLLPath = "MobileDevice.dll";


        public static bool Is64Bit()
        {
            if (IntPtr.Size == 8 || (IntPtr.Size == 4 && Is32BitProcessOn64BitProcessor()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool Is32BitProcessOn64BitProcessor()
        {
            bool retVal;
            IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);
            return retVal;
        }

        public static void InitEnviroment()
        {
            FileInfo iTunesMobileDeviceFile = null;
            DirectoryInfo ApplicationSupportDirectory = null;
            if (Is64Bit())
            {
                string dir1 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Apple Inc.\Apple Mobile Device Support", "InstallDir", "MobileDevice.dll").ToString() + "MobileDevice.dll";
                iTunesMobileDeviceFile = new FileInfo(dir1);
                string dir2 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Apple Inc.\Apple Application Support", "InstallDir", Environment.CurrentDirectory).ToString();
                ApplicationSupportDirectory = new DirectoryInfo(dir2);
            }
            else
            {
                iTunesMobileDeviceFile = new FileInfo(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Inc.\Apple Mobile Device Support\Shared", "MobileDeviceDLL", "MobileDevice.dll").ToString());
                ApplicationSupportDirectory = new DirectoryInfo(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Inc.\Apple Application Support", "InstallDir", Environment.CurrentDirectory).ToString());
            }

            string directoryName = iTunesMobileDeviceFile.DirectoryName;
            if (!iTunesMobileDeviceFile.Exists)
            {
                return;
                //throw new FileNotFoundException("Could not find iTunesMobileDevice file");
            }
            Directory.SetCurrentDirectory(directoryName);
            SetDllDirectory(ApplicationSupportDirectory.FullName);
            Environment.SetEnvironmentVariable("Path", string.Join(";", new string[] { directoryName, ApplicationSupportDirectory.FullName, Environment.GetEnvironmentVariable("Path") }));
        }

        #region Get APST Path and files
        static public string getApstPstAppleCommonDir()
        {
            return System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables("%APSTHOME%"), "PST", "Apple", "Common");
        }

        static public string getApstPhonedllResourceDir()
        {
            return System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables("%APSTHOME%"), "phonedll", "PST_APE_UNIVERSAL_USB_FD", "resource");
        }

        static public string getiDeviceUtilInAppleCommon()
        {
            return System.IO.Path.Combine(getApstPhonedllResourceDir(), "iDeviceUtil.exe");
        }
        static public string getiDeviceUtilCoreInAppleCommon()
        {
            return System.IO.Path.Combine(getApstPhonedllResourceDir(), "iDeviceUtilCore.exe");
        }
        #endregion

        static public string getApstTmpFolder()
        {
            string s = System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables("%APSTHOME%"), "temp");
            try { System.IO.Directory.CreateDirectory(s); }
            catch (Exception) { }
            return s;
        }

        static public int runExeOnly(string exeFilename, string args, int timeout = 60 * 1000, string workingDir = "")
        {
            int exitCode = -1;
            try
            {
                if (System.IO.File.Exists(exeFilename))
                {
                    Program.logIt(string.Format("[runEXE]: {0} arg={1}", exeFilename, args));
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exeFilename;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    if (!string.IsNullOrEmpty(workingDir))
                        p.StartInfo.WorkingDirectory = workingDir;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = false;

                    p.Start();
                    exitCode = 0;
                    Program.logIt(string.Format("[runEXE]: exit code={0}", exitCode));
                }
                else
                    exitCode = 2;
            }
            catch (Exception) { }
            return exitCode;
        }

        public static string[] runExe(string exeFilename, string param, out int exitCode, int timeout = 60 * 1000, string workingDir = "")
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

        public static string[] runExeBatteryCapacity(string exeFilename, string udid, out int exitCode, int timeout = 300 * 1000, string workingDir = "")
        {
            List<string> ret = new List<string>();
            exitCode = 1;
            //string param = $"-u {udid} --logcat --value powerd[";
            string param = $"-u {udid} --logcat ";
            Program.logIt($"[runExeBatteryCapacity][{udid}]: ++ exe={exeFilename}, param={param}");
            try
            {
                var regex = @"Updated Battery Health:.*?MaxCapacity:(\d+) CycleCount:";
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
                            Program.logIt($"[runExeBatteryCapacity][{udid}]: {args.Data}");
                            var health = Regex.Match(args.Data, regex);
                            if (health.Success)
                            {
                                Console.WriteLine($"MaxCapacity={health.Groups[1].Value}");
                                Program.logIt($"[runExeBatteryCapacity]: MaxCapacity={health.Groups[1].Value}");
                                string value = health.Groups[1].Value.Length == 3 ? "100" : health.Groups[1].Value;
                                ret.Add($"MaxCapacity={value}");
                                //p.Kill();
                                ev.Set();
                            }
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
                            Program.logIt($"[runExeBatteryCapacity][{udid}]: process is going to terminate.");
                            ev.Set();
                        }
                        if (ev.WaitOne(1000))
                        {
                            // stdout is colsed
                            proces_stdout_cloded = true;
                            Program.logIt($"[runExeBatteryCapacity][{udid}]: stdout pipe is going to close.");
                            if (!p.HasExited)
                            {
                                exitCode = 0;
                                p.Kill();
                                proces_has_killed = true;
                                Program.logIt($"[runExeBatteryCapacity][{udid}]: process is going to be killed due to timeout.");
                            }
                        }
                        if ((DateTime.Now - last_output).TotalMilliseconds > timeout)
                        {
                            Program.logIt($"[runExeBatteryCapacity][{udid}]: there are {timeout} milliseconds no response. timeout?");
                            // no output received within timeout milliseconds
                            if (!p.HasExited)
                            {
                                exitCode = 1460;
                                p.Kill();
                                proces_has_killed = true;
                                Program.logIt($"[runExeBatteryCapacity][{udid}]: process is going to be killed due to timeout.");
                            }
                        }
                    }
                    if (!proces_has_killed)
                        exitCode = p.ExitCode;
                }
                else
                {
                    Program.logIt($"[runExeBatteryCapacity][{udid}]: {exeFilename} not exist.");
                }
            }
            catch (Exception ex)
            {
                Program.logIt($"[runExeBatteryCapacity][{udid}]: {ex.Message}");
                Program.logIt($"[runExeBatteryCapacity][{udid}]: {ex.StackTrace}");
            }
            Program.logIt($"[runExeBatteryCapacity][{udid}]: -- ret={exitCode}");
            return ret.ToArray();
        }


        static public string[] runExeV1(string exeFilename, string args, out int exitCode, int timeout = 60 * 1000, string workingDir = "")
        {
            List<string> ret = new List<string>();
            exitCode = -1;
            Mutex mutx = new Mutex(false, string.Format("{0}{1}", Path.GetFileName(exeFilename), args.Replace(" ", "")));
            try
            {
                mutx.WaitOne(5000);
                if (System.IO.File.Exists(exeFilename))
                {
                    Program.logIt(string.Format("[runEXE]: {0} arg={1}", exeFilename, args));
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exeFilename;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    if (!string.IsNullOrEmpty(workingDir))
                        p.StartInfo.WorkingDirectory = workingDir;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            ret.Add(e.Data);
                        }
                    };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit(timeout);
                    if (!p.HasExited)
                    {
                        p.Kill();
                        exitCode = -1460;
                    }
                    else
                        exitCode = p.ExitCode;
                    Program.logIt(string.Format("[runEXE]: exit code={0}", exitCode));
                }
                else
                    exitCode = 2;
            }
            catch (Exception) { }
            finally
            {
                mutx.ReleaseMutex();
            }
            return ret.ToArray();
        }

        static public void runMonitorService()
        {
            do
            {
                ServiceController[] scServices;
                scServices = ServiceController.GetServices();
                ServiceController iscs = scServices.Where(a => a.ServiceName == "Apple Mobile Device").SingleOrDefault();
                try
                {
                    if (iscs != null)
                    {
                        ServiceController sc = new ServiceController("Apple Mobile Device");
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                        }
                    }
                    else
                    {
                        ServiceController sc = new ServiceController("Apple Mobile Device Service");
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                        }
                    }
                }
                catch (Exception)
                {
                    ServiceController sc = new ServiceController("Apple Mobile Device Service");
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                    }
                }
                Thread.Sleep(5000);
            } while (!bExit);
        }

        static public void runMonitorExe(object obj)
        {
            int exitCode = -1;
            string args = (string)obj;
            if (string.IsNullOrEmpty(args))
                args = "-start-service";

            new Thread(() => { runMonitorService(); }).Start();
            do
            {
                try
                {
                    string exeFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MonitoriDevice.exe");
                    //string args = "-start-service";
                    if (System.IO.File.Exists(exeFilename))
                    {
                        Program.logIt(string.Format("[runEXE]: {0} arg={1}", exeFilename, args));
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = exeFilename;
                        p.StartInfo.Arguments = args;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                Regex regexAdd = new Regex(@"iDeviceAdd=(.*?)$");
                                Regex regexRemove = new Regex(@"iDeviceRemove=(.*?)$");
                                Match matchAdd = regexAdd.Match(e.Data);
                                if (matchAdd.Success)
                                {
                                    string udid = matchAdd.Groups[1].Value;
                                    if (!ListDeviceInfo.ContainsKey(udid))
                                    {
                                        Util.ListDeviceInfo.TryAdd(udid, new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                                        //ThreadPool.QueueUserWorkItem(new WaitCallback(GetDeviceInfo), udid);
                                        ThreadPool.QueueUserWorkItem(new WaitCallback(Util.GetBatteryMaxCapacity), udid);
                                    }
                                }
                                else
                                {
                                    Match matchRemove = regexRemove.Match(e.Data);
                                    if (matchRemove.Success)
                                    {
                                        string udid = matchRemove.Groups[1].Value;
                                        if (ListDeviceInfo.ContainsKey(udid))
                                        {
                                            ConcurrentDictionary<String, String> ddd;
                                            ListDeviceInfo.TryRemove(udid, out ddd);
                                            Program.logIt("Removed Device");
                                        }
                                    }
                                }
                            }
                        };
                        p.Start();
                        p.BeginOutputReadLine();
                        JobManagement.Job job = new JobManagement.Job();
                        job.AddProcess(p.Handle);
                        p.WaitForExit();

                        exitCode = p.ExitCode;
                        Program.logIt(string.Format("[runEXE]: exit code={0}", exitCode));
                        Thread.Sleep(3000);
                    }
                    else
                        exitCode = 2;
                }
                catch (Exception) { }
            } while (!bExit);
        }

        public static void GetDeviceInfoWithkey(string udid, string key, string domain)
        {
            string utilpath = Util.getiDeviceUtilCoreInAppleCommon();
            int exit_code;
            Program.logIt(string.Format("[GetDeviceInfo]: ++ {0} == exepath {1}", udid, utilpath));
            string sParam = string.Format("--infokey {1} -u {0}  --domain {2}", udid, key, domain);
            if (string.IsNullOrEmpty(domain))
            {
                sParam = string.Format("--infokey {1} -u {0}", udid, key);
            }

            string[] ss = Util.runExe(utilpath, sParam, out exit_code);
            if (exit_code == 0)
            {
                ConcurrentDictionary<String, String> ddd = new ConcurrentDictionary<string, string>();
                if (ListDeviceInfo.ContainsKey(udid))
                {
                    ddd = ListDeviceInfo[udid];
                }
                else
                {
                    ListDeviceInfo.TryAdd(udid, ddd);
                }
                // good
                foreach (string s in ss)
                {
                    int pos = s.IndexOf('=');
                    if (pos > 0)
                    {
                        string k = s.Substring(0, pos);
                        string v = s.Substring(pos + 1);
                        if (ddd.ContainsKey(k)) ddd[k] = v;
                        else ddd.TryAdd(k, v);
                    }
                }

            }
        }

        public static void GetDeviceInfo(string udid, string type)
        {

            string utilpath = Util.getiDeviceUtilInAppleCommon();
            int exit_code;
            Program.logIt(string.Format("[GetDeviceInfo]: ++ {0} == exepath {1}", udid, utilpath));

            string sCmdLine = string.Format("-info -udid={0}", udid);
            if (string.Compare("infobundle", type, true) == 0)
            {
                sCmdLine = string.Format("-infobundle -udid={0}", udid);
            }else if (string.Compare("infodetect", type, true) == 0)
            {
                sCmdLine = string.Format("-infodetect -udid={0}", udid);
            }

            Program.logIt(string.Format("[GetDeviceInfo]:commandline {0} ", sCmdLine));
            string[] ss = Util.runExe(utilpath, sCmdLine, out exit_code);
            if (exit_code == 0)
            {
                ConcurrentDictionary<String, String> ddd;
                if (ListDeviceInfo.ContainsKey(udid))
                {
                    ddd = ListDeviceInfo[udid];
                }
                else
                {
                    ddd = new ConcurrentDictionary<string, string>();
                }

                // good
                foreach (string s in ss)
                {
                    int pos = s.IndexOf('=');
                    if (pos > 0)
                    {
                        string k = s.Substring(0, pos);
                        string v = s.Substring(pos + 1);
                        if (ddd.ContainsKey(k)) ddd[k] = v;
                        else ddd.TryAdd(k, v);
                    }
                }
                ListDeviceInfo.TryAdd(udid, ddd);
            }
        }

        public static void GetDeviceInfo(Object ud)
        {
            String udid = (String)ud;
            // keep get information for this device until information retrieved or device unplugged
            string utilpath = Util.getiDeviceUtilInAppleCommon();
            int exit_code;
            Program.logIt(string.Format("[GetDeviceInfo]: GetDeviceInfo: ++ {0} == exepath {1}", udid, utilpath));

            string[] ss = Util.runExe(utilpath, string.Format("-info -udid={0} ", udid), out exit_code);
            if (exit_code == 0)
            {
                ConcurrentDictionary<String, String> ddd = new ConcurrentDictionary<string, string>();
                if (ListDeviceInfo.ContainsKey(udid))
                {
                    ddd = ListDeviceInfo[udid];
                }
                else
                {
                    ListDeviceInfo.TryAdd(udid, ddd);
                }
                // good
                foreach (string s in ss)
                {
                    int pos = s.IndexOf('=');
                    if (pos > 0)
                    {
                        string k = s.Substring(0, pos).Trim();
                        string v = s.Substring(pos + 1);
                        if (String.IsNullOrEmpty(v)) v = "";
                        if (ddd.ContainsKey(k)) ddd[k] = v;
                        else
                        {
                            if (!ddd.TryAdd(k, v))
                            {
                                Program.logIt(k);
                            }
                        }
                    }
                }

            }

        }

        public static void GetBatteryMaxCapacity(Object ud)
        {
            String udid = (String)ud;
            string fn = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("APSTHOME"), ".icache", $"{udid}.json");
            bool skip = false;
            if (System.IO.File.Exists(fn))
            {
                try
                {
                    var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                    string s = System.IO.File.ReadAllText(fn);
                    Dictionary<string, object> td = jss.Deserialize<Dictionary<string, object>>(s);
                    if (td.ContainsKey("MaxCapacity"))
                    {
                        ConcurrentDictionary<String, String> ddd = new ConcurrentDictionary<string, string>();
                        if (ListDeviceInfo.ContainsKey(udid))
                        {
                            ddd = ListDeviceInfo[udid];
                        }
                        else
                        {
                            ListDeviceInfo.TryAdd(udid, ddd);
                        }
                        ddd["MaxCapacity"] = td["MaxCapacity"].ToString();
                        skip = true;
                    }
                }
                catch (Exception) { }
            }
            else
            {
                skip = true;
                System.IO.File.WriteAllText(fn, "{}");
            }
            if (!skip)
            {
                string utilpath = Util.getiDeviceUtilCoreInAppleCommon();
                int exit_code;
                Program.logIt($"[GetBatteryMaxCapacity]: ++ {udid} == exepath {utilpath}");
                if (!IsMaxCapacity)
                {
                    Program.logIt($"[GetBatteryMaxCapacity]: ++ IsMaxCapacity == {IsMaxCapacity}");
                    return;
                }

                string[] ss = Util.runExeBatteryCapacity(utilpath, udid, out exit_code);
                if (ss.Length > 0)
                {
                    ConcurrentDictionary<String, String> ddd = new ConcurrentDictionary<string, string>();
                    if (ListDeviceInfo.ContainsKey(udid))
                    {
                        ddd = ListDeviceInfo[udid];
                    }
                    else
                    {
                        ListDeviceInfo.TryAdd(udid, ddd);
                    }
                    // good
                    foreach (string s in ss)
                    {
                        int pos = s.IndexOf('=');
                        if (pos > 0)
                        {
                            string k = s.Substring(0, pos).Trim();
                            string v = s.Substring(pos + 1);
                            if (String.IsNullOrEmpty(v)) v = "";
                            if (ddd.ContainsKey(k)) ddd[k] = v;
                            else
                            {
                                if (!ddd.TryAdd(k, v))
                                {
                                    Program.logIt(k);
                                }
                            }
                        }
                    }
                    // save
                    if (ddd.ContainsKey("MaxCapacity"))
                    {
                        Dictionary<string, object> td = new Dictionary<string, object>()
                    {
                        { "MaxCapacity", ddd["MaxCapacity"]},
                        { "Date", DateTime.Now.ToString("o")},
                    };
                        var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                        string s = jss.Serialize(td);
                        System.IO.File.WriteAllText(fn, s);
                    }
                }
            }
        }
        /*
                static unsafe void NotifyCallback(ref AMDeviceNotificationCallbackInfo callback)
                {
                    Program.logIt("Device NotificationCall Type:" + callback.msg.ToString());           
                    if (callback.msg == NotificationMessage.Connected)
                    {
                        try
                        {
                            if (callback.dev_ptr == null) return;
                            IntPtr pp = AMDeviceCopyDeviceIdentifier(callback.dev_ptr);
                            if (pp == null) return;
                            string udid = CFStringRefToString(pp);
                            if (!String.IsNullOrEmpty(udid))
                            {
                                Program.logIt("Connect Device=" + udid);
                                ConcurrentDictionary<String, String> ddd = new ConcurrentDictionary<string, string>();
                                if (!ListDeviceInfo.ContainsKey(udid))
                                {
                                    ListDeviceInfo.TryAdd(udid, ddd);
                                }

                                ThreadPool.QueueUserWorkItem(new WaitCallback(GetDeviceInfo), udid);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Program.logIt(e.ToString());
                        }
                    }
                    else if (callback.msg == NotificationMessage.Disconnected)
                    {
                        try
                        {
                            if (callback.dev_ptr == null) return;
                            IntPtr pp = AMDeviceCopyDeviceIdentifier(callback.dev_ptr);
                            if (pp == null) return;
                            string udid = CFStringRefToString(pp);
                            if (!String.IsNullOrEmpty(udid))
                            {
                                Program.logIt("remove Device=" + udid);
                                if (ListDeviceInfo.ContainsKey(udid))
                                {
                                     Mutex mutex = new Mutex(false, udid);
                                     try
                                     {
                                         mutex.WaitOne();
                                         ConcurrentDictionary<String, String> ddd;
                                         ListDeviceInfo.TryRemove(udid, out ddd);
                                         Program.logIt("Removed Device");
                                     }
                                    finally
                                     {
                                         mutex.ReleaseMutex();
                                     }
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Program.logIt(e.ToString());
                        }
                    }
                 }
        * */
    }
}
