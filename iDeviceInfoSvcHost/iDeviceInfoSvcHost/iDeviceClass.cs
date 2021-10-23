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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace iDeviceInfoSvcHost
{
    class iDeviceClass
    {
        static IntPtr user_notification = IntPtr.Zero;
        //public static ConcurrentDictionary<string, IntPtr> idevices = new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        unsafe public static bool start()
        {
            bool ret = false;
            void* notification;
            ThreadPool.SetMaxThreads(40, Environment.ProcessorCount * 2);
            int err = CFManzana.MobileDevice.AMDeviceNotificationSubscribe(new CFManzana.DeviceNotificationCallback(NotifyCallback), 0, 0, 0, out notification);
            user_notification = new IntPtr(notification);
            CoreFoundation.CFLibrary.CFRunLoopRun();
            return ret;
        }


        [HandleProcessCorruptedStateExceptionsAttribute]
        unsafe public static void stop()
        {
            try
            {
                CoreFoundation.CFLibrary.CFRunLoopStop(CoreFoundation.CFLibrary.CFRunLoopGetCurrent());
                //idevices.Clear();
                Util.ListDeviceInfo.Clear();
                //CFManzana.MobileDevice.AMDeviceNotificationUnsubscribe(user_notification.ToPointer());
            }
            catch (Exception)
            {
            }
        }

 
        public static string[] getUdids()
        {
            string[] udids = Util.ListDeviceInfo.Keys.ToArray();
            return udids;
        }
 
        [HandleProcessCorruptedStateExceptionsAttribute]
        unsafe public static string getDeviceUdid(void* device)
        {
            string s = string.Empty;
            //Program.logIt("getDeviceUdid ++", false);
            try
            {
                IntPtr pp = CFManzana.MobileDevice.AMDeviceCopyDeviceIdentifier(device);
                CoreFoundation.CFString cfs = new CoreFoundation.CFString(pp);
                s = cfs.ToString();
            }
            catch (System.Exception)
            {

            }
            return s;
        }
        unsafe public static string getDeviceUdid(IntPtr device)
        {

            string ret = string.Empty;
            if (device != IntPtr.Zero)
            {
                ret = getDeviceUdid(device.ToPointer());
            }

            return ret;
        }


        public static bool idDeviceReady(string udid)
        {
            bool ret = false;
            if (Util.ListDeviceInfo.ContainsKey(udid))
                ret = true;
            return ret;
        }
        #region Callback from iTunesMobileDevice

        static void waitDevice(IntPtr device)
        {
            string udid = getDeviceUdid(device);
        }
        static void addDevices(IntPtr device)
        {
            string udid = getDeviceUdid(device);
            if (!string.IsNullOrEmpty(udid))
            {
                Program.logIt(string.Format("Add iOS device by udid: {0}", udid));
                //ThreadPool.QueueUserWorkItem(new WaitCallback(Util.GetDeviceInfo), udid);
                ThreadPool.QueueUserWorkItem(new WaitCallback(Util.GetBatteryMaxCapacity), udid);
                if (Util.ListDeviceInfo.ContainsKey(udid))
                {
                    //Util.ListDeviceInfo[udid] = device;
                }
                else
                {
                    Util.ListDeviceInfo.TryAdd(udid, new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                }
            }
            else
                Program.logIt("fail to get UDID from iOS devices.");
        }
        static unsafe void NotifyCallback(ref CFManzana.AMDeviceNotificationCallbackInfo callback)
        {
            Program.logIt("Device NotificationCall Type:" + callback.msg.ToString());  
            if (callback.msg == CFManzana.NotificationMessage.Connected)
            {
                 addDevices(new IntPtr(callback.dev));
            }
            else if (callback.msg == CFManzana.NotificationMessage.Disconnected)
            {
                string udid = getDeviceUdid(new IntPtr(callback.dev));
                if (Util.ListDeviceInfo.ContainsKey(udid))
                {
                    Program.logIt(string.Format("Remove iOS device by udid: {0}", udid));
                    ConcurrentDictionary<string, string> value;
                    Util.ListDeviceInfo.TryRemove(udid, out value);
                }
            }
        }

        static private void DfuConnectCallback(ref CFManzana.AMRecoveryDevice callback)
        {
        }

        static private void DfuDisconnectCallback(ref CFManzana.AMRecoveryDevice callback)
        {
        }

        static private void RecoveryConnectCallback(ref CFManzana.AMRecoveryDevice callback)
        {
        }

        static private void RecoveryDisconnectCallback(ref CFManzana.AMRecoveryDevice callback)
        {
        }
        #endregion

        #region iDevice instance
        IntPtr _device = IntPtr.Zero;
        List<string> _device_info_keys = new List<string>();
        iDeviceClass(IntPtr device)
        {
            _device = device;

        }
        public string getUdid()
        {
            return getDeviceUdid(_device);
        }
        unsafe public bool checkTrust()
        {
            bool ret = true;
            int err;
            err = CFManzana.MobileDevice.AMDeviceConnect(_device.ToPointer());
            if (err == 0)
            {
                string s = CFManzana.MobileDevice.AMDeviceCopyValue(_device.ToPointer(), "ProductVersion");
                Version v = new Version(s);
                s = getUdid();

                if (v >= new Version("7.0"))
                {
                    //ret = false;
                    if (!System.IO.File.Exists(System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "apple", "lockdown", s + ".plist")))
                        ret = false;
                }

                CFManzana.MobileDevice.AMDeviceDisconnect(_device.ToPointer());
            }
            return ret;
        }
        unsafe public int connect()
        {
            int ret = 0;
            int err;
            err = CFManzana.MobileDevice.AMDeviceConnect(_device.ToPointer());
            err = CFManzana.MobileDevice.AMDeviceIsPaired(_device.ToPointer());
            if (err == 1)
            {
                // paired on this pc
                err = CFManzana.MobileDevice.AMDeviceValidatePairing(_device.ToPointer());
                if (err != 0)
                {
                    err = CFManzana.MobileDevice.AMDevicePair(_device.ToPointer());
                }
            }
            else
            {
                // not paired on this pc
                err = CFManzana.MobileDevice.AMDevicePair(_device.ToPointer());
            }

            if (err == 0)
            {
                // start lock down service
                err = CFManzana.MobileDevice.AMDeviceStartSession(_device.ToPointer());
                ret = err;

                // end
            }
            else
            {
                ret = err;
            }
            return ret;
        }
        unsafe public void disconnect()
        {
            int err;
            err = CFManzana.MobileDevice.AMDeviceStopSession(_device.ToPointer());
            err = CFManzana.MobileDevice.AMDeviceDisconnect(_device.ToPointer());
        }
        unsafe public string copyValue(string key)
        {
            return CFManzana.MobileDevice.AMDeviceCopyValue(_device.ToPointer(), key);
        }

        unsafe public string copyValue(string domain, string key)
        {
            return CFManzana.MobileDevice.AMDeviceCopyValue(_device.ToPointer(), domain, key);
        }


        // end
        public int deactive()
        {
            return CFManzana.MobileDevice.AMDeviceDeactivate(_device);
        }
        unsafe public int active(string[] keys, IntPtr[] values)
        {
            return CFManzana.MobileDevice.AMDeviceActivate(_device.ToPointer(), new CoreFoundation.CFDictionary(keys, values));
        }
        unsafe public IntPtr copyValue_IntPtr(string key)
        {
            return CFManzana.MobileDevice.AMDeviceCopyValue_IntPtr(_device.ToPointer(), 0, new CoreFoundation.CFString(key));
        }
        #endregion

    }
}
