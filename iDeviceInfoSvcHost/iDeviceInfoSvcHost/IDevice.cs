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
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace iDeviceInfoSvcHost
{
    [ServiceContract]
    public interface IDevice
    {
        //[OperationContract(Name="enum")]
        //[WebGet]
        [OperationContract]
        [WebGet(UriTemplate = "device/enum")]
        Stream enumDevice();

        //[OperationContract(Name = "information")]
        //[WebGet]
        [OperationContract]
        [WebGet(UriTemplate = "device/information?id={id}&force={force}&type={type}")]
        Stream getDeviceInformation(string id, string force, string type);

        [OperationContract]
        [WebGet(UriTemplate = "device/info?id={id}&key={key}&domain={domain}")]
        Stream getDeviceInfoWithKey(string id, string key, string domain);
    }
}
