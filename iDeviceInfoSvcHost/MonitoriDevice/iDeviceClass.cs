using MonitoriDevice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace MonitoriDevice
{
    class iDeviceClass
    {
        static IntPtr user_notification = IntPtr.Zero;
        //public static ConcurrentDictionary<string, IntPtr> idevices = new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        static IntPtr curRunLoopRef;
        unsafe public static bool start()
        {
            bool ret = false;
            void* notification;
            int err = CFManzana.MobileDevice.AMDeviceNotificationSubscribe(new CFManzana.DeviceNotificationCallback(NotifyCallback), 0, 0, 0, out notification);
            user_notification = new IntPtr(notification);
            curRunLoopRef = CoreFoundation.CFLibrary.CFRunLoopGetCurrent();
            CoreFoundation.CFLibrary.CFRunLoopRun();
            return ret;

            //bool ret = true;
            //IntPtr err = IntPtr.Zero;
            //int notification = 0;
            //notification = CFManzana.MobileDevice.AMRestorableDeviceRegisterForNotificationsForDevices(new CFManzana.NotificationDevicesCallback(EventHandler), IntPtr.Zero, 79, out err);
            //if (err == IntPtr.Zero)
            //    return ret;
            //else
            //{
            //    notification = CFManzana.MobileDevice.AMRestorableDeviceRegisterForNotifications(new CFManzana.NotificationDevicesCallback(EventHandler), IntPtr.Zero, out err);
            //    if (err != IntPtr.Zero)
            //    {
            //        ret = false;
            //    }
            //}


            //return ret;

        }
        [HandleProcessCorruptedStateExceptionsAttribute]
        unsafe public static void stop()
        {
            try
            {
                //idevices.Clear();
                //CFManzana.MobileDevice.AMDeviceNotificationUnsubscribe(user_notification.ToPointer());
                CoreFoundation.CFLibrary.CFRunLoopStop(curRunLoopRef);
            }
            catch (Exception)
            {
            }
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
                Program.logIt("iDeviceAdd="+udid);
            }
            else
                Program.logIt("fail to get UDID from iOS devices.");
        }

        static unsafe void EventHandler(IntPtr device, int type, IntPtr arg)
        {
            const int DEV_CONNECTED = 0;
            const int DEV_DISCONNECTED = 1;
            const int DEV_STATE_DFUMODE = 1;
            const int DEV_STATE_RECOVERY = 2;
            const int DEV_STATE_RESTORABLE = 3;
            const int DEV_STATE_NORMAL = 4;

            int device_state = CFManzana.MobileDevice.AMRestorableDeviceGetState(device);
            if (type == DEV_CONNECTED)
            {
                switch (device_state)
                {
                    case DEV_STATE_DFUMODE:// Dfu mode
                        break;
                    case DEV_STATE_RECOVERY:
                        {
 //                           CFStringRef srnm = g_iTunesMobileDevice.AMRestorableDeviceCopySerialNumber(_device);
 //                           AMRecoveryModeDeviceRef device = g_iTunesMobileDevice.AMRestorableDeviceCopyRecoveryModeDevice(_device);
                        }
                        break;
                    case DEV_STATE_RESTORABLE:
                        {
 //                           CFStringRef srnm = g_iTunesMobileDevice.AMRestorableDeviceCopySerialNumber(_device);
 //                           AMRestoreModeDeviceRef device = g_iTunesMobileDevice.AMRestorableDeviceCopyRestoreModeDevice(_device);
                        }
                        break;
                    case DEV_STATE_NORMAL:
                        {
                            IntPtr dev = CFManzana.MobileDevice.AMRestorableDeviceCopyAMDevice(device);
                            if (dev != IntPtr.Zero)
                            {
                                addDevices(device);
                            }
                        }
                        break;
                    default:
                        break;
                }

            }
            else if (type == DEV_DISCONNECTED)
            {
                switch (device_state)
                {
                    case DEV_STATE_DFUMODE:// Dfu mode
                        break;
                    case DEV_STATE_RECOVERY:
                        {
                        }
                        break;
                    case DEV_STATE_RESTORABLE:
                        {
                        }
                        break;
                    case DEV_STATE_NORMAL:
                        {
                            IntPtr dev = CFManzana.MobileDevice.AMRestorableDeviceCopyAMDevice(device);
                            if (dev != IntPtr.Zero)
                            {
                                string udid = getDeviceUdid(device);
                                Program.logIt("iDeviceRemove=" + udid);
                            }
                        }
                        break;
                    default:
                        break;
                }

            }
            else
            {

            }
            /*
             * #define DEV_CONNECTED		0
#define DEV_DISCONNECTED	1
#define DEV_STATE_DFUMODE		1
#define DEV_STATE_RECOVERY		2
#define DEV_STATE_RESTORABLE	3
#define DEV_STATE_NORMAL		4
	if (type==DEV_CONNECTED)
	{
		// connect
		int device_state=g_iTunesMobileDevice.AMRestorableDeviceGetState(_device);
		switch(device_state)
		{
		case DEV_STATE_DFUMODE:// Dfu mode
			break;
		case DEV_STATE_RECOVERY:
			{
				TCHAR buffer[MAX_PATH]={0};
				CFStringRef srnm=g_iTunesMobileDevice.AMRestorableDeviceCopySerialNumber(_device);
				AMRecoveryModeDeviceRef device = g_iTunesMobileDevice.AMRestorableDeviceCopyRecoveryModeDevice(_device);
				if (srnm!=NULL)
				{
					CFStringGetCString(srnm, buffer, MAX_PATH, kCFStringEncodingUTF8);
					logIt(buffer);
					if (device!=NULL)
					{
						g_args.deivces_recovery.add_device(buffer, device);
					}
				}
			}
			break;
		case DEV_STATE_RESTORABLE:
			{
				TCHAR buffer[MAX_PATH]={0};
				CFStringRef srnm=g_iTunesMobileDevice.AMRestorableDeviceCopySerialNumber(_device);
				AMRestoreModeDeviceRef device=g_iTunesMobileDevice.AMRestorableDeviceCopyRestoreModeDevice(_device);
				if (srnm!=NULL)
				{
					CFStringGetCString(srnm, buffer, MAX_PATH, kCFStringEncodingUTF8);
					logIt(buffer);
					if (device!=NULL)
					{
						g_args.deivces_restore.add_device(buffer, device);
					}
				}
			}
			break;
		case DEV_STATE_NORMAL:
			{
				char _udid[MAX_PATH];
				ZeroMemory(_udid,sizeof(_udid));
				void* device=g_iTunesMobileDevice.AMRestorableDeviceCopyAMDevice(_device);
				if (device!=NULL)
				{
					CFStringRef udid=g_iTunesMobileDevice.AMDeviceCopyDeviceIdentifier(device);
					if(udid!=NULL)
					{
						CFStringGetCString(udid, _udid, 128, kCFStringEncodingUTF8);
						g_args.deivces.add_device(_udid, device);
						CF_RELEASE_CLEAR(udid);
					}
				}
			}
			break;
		default:
			break;
		}
	}
	else if (type==DEV_DISCONNECTED)
	{
		// disconnect
		int device_state=g_iTunesMobileDevice.AMRestorableDeviceGetState(_device);
		switch(device_state)
		{
		case DEV_STATE_DFUMODE:// Dfu mode
			break;
		case DEV_STATE_RECOVERY:
			{
				TCHAR buffer[MAX_PATH]={0};
				CFStringRef srnm=g_iTunesMobileDevice.AMRestorableDeviceCopySerialNumber(_device);
				if (srnm!=NULL)
				{
					CFStringGetCString(srnm, buffer, MAX_PATH, kCFStringEncodingUTF8);
					g_args.deivces_recovery.remove_device(buffer);
				}
			}
			break;
		case DEV_STATE_RESTORABLE:
			{
				TCHAR buffer[MAX_PATH]={0};
				CFStringRef srnm=g_iTunesMobileDevice.AMRestorableDeviceCopySerialNumber(_device);
				if (srnm!=NULL)
				{
					CFStringGetCString(srnm, buffer, MAX_PATH, kCFStringEncodingUTF8);
					g_args.deivces_restore.remove_device(buffer);
				}
			}
			break;
		case DEV_STATE_NORMAL:
			{
				char _udid[MAX_PATH];
				ZeroMemory(_udid,sizeof(_udid));
				HANDLE device=g_iTunesMobileDevice.AMRestorableDeviceCopyAMDevice(_device);
				if (device!=NULL)
				{
					CFStringRef udid=g_iTunesMobileDevice.AMDeviceCopyDeviceIdentifier(device);
					if(udid!=NULL)
					{
						CFStringGetCString(udid, _udid, 128, kCFStringEncodingUTF8);
						g_args.deivces.remove_device(_udid);
						CF_RELEASE_CLEAR(udid);
					}
				}
			}
			break;
		default:
			break;
		}
	}
	else
	{
		// ??
	}

             */
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
                Program.logIt("iDeviceRemove=" + udid);
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
                // paired on this PC
                err = CFManzana.MobileDevice.AMDeviceValidatePairing(_device.ToPointer());
                if (err != 0)
                {
                    err = CFManzana.MobileDevice.AMDevicePair(_device.ToPointer());
                }
            }
            else
            {
                // not paired on this PC
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
