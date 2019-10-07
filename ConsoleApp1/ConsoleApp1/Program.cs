using StackExchange.Redis;
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
            //test();
            test_redis();
        }

        static void test()
        {
            System.Net.NetworkInformation.NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach(System.Net.NetworkInformation.NetworkInterface nic in nics)
            {

            }
        }

        static void test_redis()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            if(!db.KeyExists("mykey"))
            {
                db.StringSet("mykey", "init_value");
            }
            string value = db.StringGet("mykey");
            
        }
    }
}
