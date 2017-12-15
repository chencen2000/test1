using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Collections.Specialized.StringDictionary _args = util.parseCommandLines(args);
            System.Tuple<Boolean,int,String[]> ret=util.runExe("cmd.exe", "/c dir");
            Console.WriteLine("Hello World!");
        }
    }
}
