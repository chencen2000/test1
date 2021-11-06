#include "pch.h"
#include <atlstr.h>
using namespace System;

char* TAG = "FDUSBDEVICECLEANUP";
char* log_filename = NULL;

void LogIt(String^ msg) {
    System::DateTime^ t0 = System::DateTime::Now;
    String^ stag = gcnew String(TAG);
    String^ s = String::Format("[{0}]: [{1}]: {2}", stag, t0->ToString("o"), msg);
    System::Diagnostics::Trace::WriteLine(s);
    if (log_filename != NULL) {
        String^ slog = gcnew String(log_filename);
        System::IO::File::AppendAllText(slog, s+"\n");
    }
}

void LogItW(WCHAR* msg) {
    String^ smsg = gcnew String(msg);
    LogIt(smsg);
}

void LogItA(char* msg) {
    String^ smsg = gcnew String(msg);
    LogIt(smsg);
}

int start_service(System::Collections::Specialized::StringDictionary^ args, System::Threading::EventWaitHandle^ quitEvent) {
    int ret = 0;
    LogIt("start_service: ++");
    System::Collections::ArrayList^ devices = listAllDevices();
    DateTime t_list = System::DateTime::Now;
    for (int i = 0; i < devices->Count; i++) {
        LogIt(System::String::Format("{0:D5}: {1}", i, devices[i]));
    }
    System::Diagnostics::PerformanceCounter^ pc = gcnew System::Diagnostics::PerformanceCounter("Processor", "% Processor Time", "_Total");
    float cpu_usage = pc->NextValue();
    bool done = FALSE;
    int method = 1;
    if (args->ContainsKey("method")) {
        if (!System::Int32::TryParse(args["method"], method))
            method = 1;
    }
    int interval = 1000;
    if (args->ContainsKey("interval"))
    {
        if (!System::Int32::TryParse(args["interval"], interval))
            interval = 1000;
    }
    int list_device_interval = 60;
    if (args->ContainsKey("listinterval"))
    {
        if (!System::Int32::TryParse(args["listinterval"], list_device_interval))
            list_device_interval = 60;
    }
    int cpu_busy_interval = 5000;
    if (args->ContainsKey("cpubusyinterval"))
    {
        if (!System::Int32::TryParse(args["cpubusyinterval"], cpu_busy_interval))
            cpu_busy_interval = 5000;
    }
    int cpu_threshold = 80;
    if (args->ContainsKey("cpuhreshold"))
    {
        if (!System::Int32::TryParse(args["cpuhreshold"], cpu_threshold))
            cpu_threshold = 80;
    }
    int delete_device_threshold = 500;
    if (args->ContainsKey("deletethreshold"))
    {
        if (!System::Int32::TryParse(args["deletethreshold"], delete_device_threshold))
            delete_device_threshold = 500;
    }
    LogIt("Dump paramers:");
    LogIt(System::String::Format("method={0}", method));
    LogIt(System::String::Format("interval={0}ms", interval));
    LogIt(System::String::Format("listinterval={0}seconds", list_device_interval));
    LogIt(System::String::Format("cpubusyinterval={0}ms", cpu_busy_interval));
    LogIt(System::String::Format("cpuhreshold={0}%", cpu_threshold));
    LogIt(System::String::Format("deletethreshold={0}ms", delete_device_threshold));
    while (!done) {
        cpu_usage = pc->NextValue();
        LogIt(System::String::Format("CPU usage is {0:F2}%", cpu_usage));
        if (System::Console::KeyAvailable)
        {
            LogIt("Program is going to terminate by keyboard click.");
            done = TRUE;
            continue;
        }
        if (quitEvent->WaitOne(interval))
        {
            LogIt("Program is going to terminate by event set.");
            done = TRUE;
            continue;
        }
        if (cpu_usage > cpu_threshold) {
            LogIt(System::String::Format("CPU usage ({0:F2}%) is too high.", cpu_usage));
            if (interval < cpu_busy_interval)
                interval = cpu_busy_interval;
            continue;
        }
        if (devices->Count > 0) {
            String^ d = (String^)devices[0];
            System::DateTime t0 = System::DateTime::Now;
            if (method == 1) {
                if (removeDeviceByInstanceId(d) == NOERROR) {
                    devices->Remove(d);
                }
            }
            else if (method == 2) {
                if (removeDeviceByInstanceId2(d) == NOERROR) {
                    devices->Remove(d);
                }
            }
            System::TimeSpan ts = System::DateTime::Now - t0;
            LogIt(System::String::Format("It took {0} ms to remove the device.", ts.TotalMilliseconds));
            if (ts.TotalMilliseconds < delete_device_threshold) {
                interval = 0;
            }
            else {
                interval = cpu_busy_interval;
                LogIt(System::String::Format("Delete device took too long. {0} > {1}. Set interval higher {2}", ts.TotalMilliseconds, delete_device_threshold, cpu_busy_interval));
            }
        }
        else {            
            if ((System::DateTime::Now - t_list).TotalSeconds > list_device_interval) {
                devices = listAllDevices();
                for (int i = 0; i < devices->Count; i++) {
                    LogIt(System::String::Format("{0:D5}: {1}", i, devices[i]));
                }
                t_list = System::DateTime::Now;
            }
        }
    }
    LogIt("start_service: --");
    return ret;
}
int main(array<System::String^>^ args)
{
    int ret = 0;
    System::Configuration::Install::InstallContext^ _args = gcnew System::Configuration::Install::InstallContext(nullptr, args);
    String^ stag = gcnew String(TAG);
    if (_args->IsParameterTrue("debug")) {
        System::Console::WriteLine("Wait for debugger, press any key to contibue...");
        System::Console::ReadKey();
    }
    if (_args->IsParameterTrue("start-service")) {
        bool own;
        System::Threading::EventWaitHandle^ e = gcnew System::Threading::EventWaitHandle(false, System::Threading::EventResetMode::AutoReset, stag, own);
        if (own) {
            if (_args->Parameters->ContainsKey("log")) {
                log_filename = (char*)(void*)System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(_args->Parameters["log"]);
            }
            ret = start_service(_args->Parameters, e);
            e->Close();
            if (log_filename != NULL) {
                System::Runtime::InteropServices::Marshal::FreeHGlobal((System::IntPtr)log_filename);
            }
        }
        else {
            LogIt("Service already running.");
            e->Close();
            ret = 2;
        }
    }
    else if (_args->IsParameterTrue("kill-service")) {    
        try
        {
            System::Threading::EventWaitHandle^ e = System::Threading::EventWaitHandle::OpenExisting(stag);
            if (e != nullptr) {
                e->Set();
            }
        }
        catch (System::Exception^ e)
        {

        }
    }
    else {
        // test
		System::String^ ss = "Managed String";
		CStringA cs = ss;
		cs.AppendFormat("\nAppend CString.");
		ss = gcnew System::String(cs);
		System::Console::WriteLine(ss);
    }
    return ret;
}
