using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Collections.Specialized.StringDictionary _args = util.parseCommandLines(args);
            //System.Tuple<Boolean,int,String[]> ret=util.runExe("cmd.exe", "/c dir");
            Console.WriteLine("Hello World!");
            test();
        }

        static void test()
        {
            System.Net.NetworkInformation.NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach(System.Net.NetworkInformation.NetworkInterface nic in nics)
            {

            }
        }
    }
}
