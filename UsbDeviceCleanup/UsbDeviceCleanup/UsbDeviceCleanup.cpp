#include "pch.h"
#include <SetupAPI.h>
#include <strsafe.h>
#include <cfgmgr32.h>
#include <vcclr.h>

int removeDeviceByInstanceId(System::String^ deviceInstanceId) {
	int ret = ERROR_INVALID_PARAMETER;	
	LogIt(System::String::Format("removeDeviceByInstanceId: [{0}] ++ ", deviceInstanceId));
	char* diid = (char*)(void*)System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(deviceInstanceId);
	if (diid != NULL) {
		HANDLE h = SetupDiCreateDeviceInfoList(NULL, NULL);
		if (h == INVALID_HANDLE_VALUE) {
			ret = GetLastError();
		}
		else {
			SP_DEVINFO_DATA devInfoData;
			ZeroMemory(&devInfoData, sizeof(SP_DEVINFO_DATA));
			devInfoData.cbSize = sizeof(SP_DEVINFO_DATA);
			if (SetupDiOpenDeviceInfoA(h, diid, NULL, 0, &devInfoData)) {
				ULONG status = 0;
				ULONG problem = 0;
				ret = CM_Get_DevNode_Status(&status, &problem, devInfoData.DevInst, 0);
				if (ret == CR_NO_SUCH_DEVNODE) {
					SP_REMOVEDEVICE_PARAMS rdp;
					ZeroMemory(&rdp, sizeof(SP_REMOVEDEVICE_PARAMS));
					rdp.ClassInstallHeader.cbSize = sizeof(SP_CLASSINSTALL_HEADER);
					rdp.ClassInstallHeader.InstallFunction = DIF_REMOVE;
					rdp.Scope = DI_REMOVEDEVICE_GLOBAL;
					if (SetupDiSetClassInstallParams(h, &devInfoData, &rdp.ClassInstallHeader, sizeof(SP_REMOVEDEVICE_PARAMS))) {
						if (SetupDiCallClassInstaller(DIF_REMOVE, h, &devInfoData)) {
							SP_DEVINSTALL_PARAMS params;
							params.cbSize = sizeof(SP_DEVINSTALL_PARAMS);
							if (SetupDiGetDeviceInstallParams(h, &devInfoData, &params)) {
								ret = NO_ERROR;
							}
							else
								ret = GetLastError();
						}
						else
							ret = GetLastError();
					}
					else
						ret = GetLastError();
				}
			}
			else {
				ret = GetLastError();
			}
			SetupDiDestroyDeviceInfoList(h);
		}
		System::Runtime::InteropServices::Marshal::FreeHGlobal((System::IntPtr)diid);
	}
	LogIt(System::String::Format("removeDeviceByInstanceId: [{0}] -- ret={1}", deviceInstanceId, ret));
	return ret;
}

System::Collections::ArrayList^ listAllDevices() {
	DWORD ret = ERROR_INVALID_PARAMETER;
	BYTE buffer[1024];
	LogItW(L"listAllDevices: ++ ");
	System::Collections::ArrayList^ list = gcnew System::Collections::ArrayList();
	HANDLE h = SetupDiGetClassDevs(NULL, NULL, NULL, DIGCF_ALLCLASSES);
	if (h != INVALID_HANDLE_VALUE) {
		SP_DEVINFO_DATA devInfoData;
		devInfoData.cbSize = sizeof(SP_DEVINFO_DATA);
		DWORD idx = 0;
		DWORD sz = 0;
		CONFIGRET cr = 0;
		ULONG status = 0;
		ULONG problem = 0;
		while (SetupDiEnumDeviceInfo(h, idx++, &devInfoData)) {
			cr = CM_Get_DevNode_Status(&status, &problem, devInfoData.DevInst, 0);
			if (SetupDiGetDeviceInstanceIdA(h, &devInfoData, (char*)buffer, MAX_PATH, &sz)) {
				//LogItW((PWSTR)buffer);
				if (cr == CR_NO_SUCH_DEVINST) {
					System::String^ s = System::Runtime::InteropServices::Marshal::PtrToStringAnsi(static_cast<System::IntPtr>((char*)buffer));
					list->Add(s);
				}
			}
		}
		SetupDiDestroyDeviceInfoList(h);
		ret = NO_ERROR;
	}
	else
		ret = GetLastError();
	StringCchPrintfW((WCHAR*)buffer, 500, L"listAllDevices: -- ret=%d", ret);
	LogItW((WCHAR*)buffer);
	return list;
}

