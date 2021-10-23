using iDeviceInfoSvcHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MonitoriDevice
{
    class Program
    {
        static public void logIt(string s)
        {
            Console.WriteLine(string.Format("[{0}]: [{1}]: {2}", DateTime.Now, System.Threading.Thread.CurrentThread.ManagedThreadId, s));
            System.Diagnostics.Trace.WriteLine(string.Format("[DeviceInfo]: [{0}]: {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, s));
        }

        const string IdeviceInfoSvcHost_Event_Name = "IDeviceMonitorPlugEVENT_Jeffery";

        static System.Threading.EventWaitHandle ewait = null;

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

            System.AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            if (_arg.IsParameterTrue("start-service"))
            {
                // start service
                Boolean bFind = false;
                try
                {
                    ewait = System.Threading.EventWaitHandle.OpenExisting(IdeviceInfoSvcHost_Event_Name);
                    ewait.Close();
                    logIt("Instance already started.");
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    bFind = true;
                  
                }
                catch (Exception) { }
                if (bFind)
                {
                    try
                    {
                        ewait = new EventWaitHandle(false, EventResetMode.ManualReset, IdeviceInfoSvcHost_Event_Name);
                        //Util.InitEnviroment();
                        
                        new Thread(() =>
                        {
                            iDeviceClass.start();

                        }).Start();
                        Console.WriteLine(@"Press any key to terminate...");
                        while (!ewait.WaitOne(1000))
                        {
                            if (System.Console.KeyAvailable)
                                ewait.Set();
                        }
                        ewait.Close();
                        iDeviceClass.stop();

                    }
                    catch (Exception)
                    {

                    }
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
                System.Console.WriteLine("MonitoriDevice.exe");
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
           //
        }
    }
}
