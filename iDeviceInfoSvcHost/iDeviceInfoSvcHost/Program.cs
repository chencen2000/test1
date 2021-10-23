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
using System.Runtime.ExceptionServices;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace iDeviceInfoSvcHost
{
    class Program
    {
        static public void logIt(string s)
        {
            lock (Util.ListDeviceInfo)
            {
                Console.WriteLine(string.Format("[{0}]: [{1}]: {2}", DateTime.Now, System.Threading.Thread.CurrentThread.ManagedThreadId, s));
                System.Diagnostics.Trace.WriteLine(string.Format("[DeviceInfo]: [{0}]: {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, s));
            }
        }

        const string IdeviceInfoSvcHost_Event_Name = "IDeviceInfoSvcHostEVENT_Jeffery";

        static System.Threading.EventWaitHandle ewait = null;

        [HandleProcessCorruptedStateExceptionsAttribute]
        [STAThread]
        static void Main(string[] args)
        {
            System.Configuration.Install.InstallContext _arg = new System.Configuration.Install.InstallContext(null, args);
            if (_arg.IsParameterTrue("debug"))
            {
                System.Console.WriteLine("Wait for debugger, press any key to continue...");
                System.Console.ReadKey();
            }
            // dump version
            logIt(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.ToString());
            // dump args
            logIt(string.Format("called by arg: ({0})", args.Length));
            foreach (string s in args)
                logIt(s);


            IniFile ini = new IniFile(Path.Combine(Environment.ExpandEnvironmentVariables(@"%APSTHOME%"), "config.ini"));
            String sMaxcapacity = ini.GetString("battery", "read_ratio_reboot", "false");
            Util.IsMaxCapacity = String.Compare(sMaxcapacity, "true", true) == 0 || String.Compare(sMaxcapacity, "1", true) == 0;
            //Util.IsMaxCapacity = true;
            logIt($"config Maxcapacity = {sMaxcapacity}:{Util.IsMaxCapacity}");
            System.AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            if (_arg.IsParameterTrue("start-service"))
            {
                // start service
                Boolean bRun = false;
                try
                {
                    ewait = System.Threading.EventWaitHandle.OpenExisting(IdeviceInfoSvcHost_Event_Name);
                    ewait.Close();
                    logIt("Instance already started.");
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    bRun = true;                   
                }
                catch (Exception) { }
                if (bRun)
                {
                    try {
                        ewait = new EventWaitHandle(false, EventResetMode.ManualReset, IdeviceInfoSvcHost_Event_Name);
                        //Util.InitEnviroment();
                        ThreadPool.QueueUserWorkItem(new WaitCallback(Util.runMonitorExe), null);
                        using (ServiceHost host = new ServiceHost(typeof(Device)))
                        {
                            host.Open();
                            Console.WriteLine(@"go to http://localhost:1930/device to test");
                            Console.WriteLine(@"Press any key to terminate...");
                            while (!ewait.WaitOne(1000))
                            {
                                if (System.Console.KeyAvailable)
                                    ewait.Set();
                            }
                            host.Close();
                        }
                        Util.bExit = true;
                        Util.runMonitorExe("-kill-service");
                        ewait.Close();
                    } catch (Exception)
                    {
                        logIt("iTunes MobileDevice.Dll not found.************");
                    }
                    Util.bExit = true;
                    Util.runMonitorExe("-kill-service");
                    ewait.Close();
                }
            }
            else if (_arg.IsParameterTrue("kill-service"))
            {
                // stop service
                try
                {
                    ewait = System.Threading.EventWaitHandle.OpenExisting(IdeviceInfoSvcHost_Event_Name);
                    if (ewait != null)
                        ewait.Set();
                }
                catch (Exception) { }
            }
            else
            {
                System.Console.WriteLine("IdeviceInfoSvcHost.exe");
                System.Console.WriteLine("-start-service: to start the service");
                System.Console.WriteLine("-kill-service: to stop the service");
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logIt(e.ToString());
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;
            logIt(ex.StackTrace);
            if (ewait != null)
            {
                ewait.Set();
                //ewait.Close();
            }
            Thread.Sleep(5000);
            Util.runExeOnly(System.Reflection.Assembly.GetEntryAssembly().Location, "-start-service");
            Environment.Exit(1000);
        }
    }
}
