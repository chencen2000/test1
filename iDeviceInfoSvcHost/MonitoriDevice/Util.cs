using Microsoft.Win32;
using MonitoriDevice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace iDeviceInfoSvcHost
{
/*

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DeviceNotificationCallback(ref AMDeviceNotificationCallbackInfo callback_info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DeviceRestoreNotificationCallback(ref AMRecoveryDevice callback_info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DeviceRestoreNotificationCallbackV2(IntPtr device);


    /// <summary>
    /// Provides the fields representing the type of notification
    /// </summary>
    public enum NotificationMessage
    {
        /// <summary>The iDevice was connected to the computer.</summary>
        Connected = 1,
        /// <summary>The iDevice was disconnected from the computer.</summary>
        Disconnected = 2,

        /// <summary>Notification from the iDevice occurred, but the type is unknown.</summary>
        Unknown = 3,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AMDeviceNotification
    {
        private uint unknown0;

        private uint unknown1;

        private uint unknown2;

        private DeviceNotificationCallback callback;

        private uint unknown3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AMDeviceNotificationCallbackInfo
    {
        internal IntPtr dev_ptr;

        public NotificationMessage msg;
    }
    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //public struct AMDeviceNotificationCallbackInfo
    //{
    //    unsafe public void* dev
    //    {
    //        get
    //        {
    //            return dev_ptr;
    //        }
    //    }
    //    unsafe internal void* dev_ptr;
    //    public NotificationMessage msg;
    //}

*/
    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //public struct AMRecoveryDevice
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    //    public byte[] unknown0;			/* 0 */
    //    public DeviceRestoreNotificationCallback callback;		/* 8 */
    //    public IntPtr user_info;			/* 12 */
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    //    public byte[] unknown1;			/* 16 */
    //    public uint readwrite_pipe;		/* 28 */
    //    public byte read_pipe;          /* 32 */
    //    public byte write_ctrl_pipe;    /* 33 */
    //    public byte read_unknown_pipe;  /* 34 */
    //    public byte write_file_pipe;    /* 35 */
    //    public byte write_input_pipe;   /* 36 */
    //};


    public class Util
    {

        public static ConcurrentDictionary<String, ConcurrentDictionary<String, String>> ListDeviceInfo = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

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
            if (Is64Bit() == true)
            {
                string dir1 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Apple Inc.\Apple Mobile Device Support","InstallDir","MobileDevice.dll").ToString() + "MobileDevice.dll";
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
                throw new FileNotFoundException("Could not find iTunesMobileDevice file");
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
            return System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables("%APSTHOME%"), "phonedll","PST_APE_UNIVERSAL_USB_FD", "resource");
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
        //[DllImport(DLLPath, CallingConvention = CallingConvention.Cdecl)]
        //unsafe public extern static int AMDeviceNotificationSubscribe(DeviceNotificationCallback callback, uint unused1, uint unused2, uint unused3, out IntPtr am_device_notification_ptr);

        //[DllImport(DLLPath, CallingConvention = CallingConvention.Cdecl)]
        //unsafe public extern static void AMDeviceNotificationUnsubscribe(IntPtr am_device_notification_ptr);

        //[DllImport(DLLPath, CallingConvention = CallingConvention.Cdecl)]
        //unsafe public extern static IntPtr AMDeviceCopyDeviceIdentifier(IntPtr device);


        //static IntPtr user_notification = IntPtr.Zero;
        //unsafe public static bool start()
        //{
        //    bool ret = false;

        //    int err = AMDeviceNotificationSubscribe(new DeviceNotificationCallback(NotifyCallback), 0, 0, 0, out user_notification);
        //    return ret;
        //}
        //unsafe public static void stop()
        //{
        //    try
        //    {
        //        AMDeviceNotificationUnsubscribe(user_notification);
        //    }
        //    catch (Exception e)
        //    {
        //        e.ToString();
        //    }
        //}

        //static string CFStringRefToString(IntPtr stringRef)
        //{
        //    string result = "";
        //    if (stringRef == null) return result;
        //    try
        //    {
        //        result = Marshal.PtrToStringAnsi(new IntPtr(stringRef.ToInt64() + 9L));
        //    }
        //    catch (Exception)
        //    {
        //        result = "";
        //    }
        //    return result;
        //}

        static public int runExeOnly(string exeFilename, string args, int timeout = 60*1000, string workingDir = "")
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

        static public string[] runExe(string exeFilename, string args, out int exitCode, int timeout = 60*1000, string workingDir = "")
        {
            List<string> ret = new List<string>();
            exitCode = -1;
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
            return ret.ToArray();
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

        public static void GetDeviceInfo(string  udid, string type)
        {

            string utilpath = Util.getiDeviceUtilInAppleCommon();
            int exit_code;
            Program.logIt(string.Format("[GetDeviceInfo]: ++ {0} == exepath {1}", udid, utilpath));

            string sCmdLine = string.Format("-info -udid={0}", udid);
            if (string.Compare("", type, true) == 0)
            {
                sCmdLine = string.Format("-infobundle -udid={0}", udid);
            }

            string[] ss = Util.runExe(utilpath, sCmdLine, out exit_code);
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

        public static void GetDeviceInfo(Object  ud)
        {
            String udid = (String)ud;
        // keep get information for this device until information retrieved or device unplugged
            string utilpath = Util.getiDeviceUtilInAppleCommon();
            int exit_code;
            Program.logIt(string.Format("[GetDeviceInfo]: ++ {0} == exepath {1}", udid, utilpath));

            string[] ss = Util.runExe(utilpath, string.Format("-info -udid={0} ", udid), out exit_code);
            if (exit_code == 0)
            {
                ConcurrentDictionary<String, String> ddd = new ConcurrentDictionary<string, string>();
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
                if (ListDeviceInfo.ContainsKey(udid))
                {
                    ListDeviceInfo[udid] = ddd;
                }
                else
                {
                    ListDeviceInfo.TryAdd(udid, ddd);
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
