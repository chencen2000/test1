using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    class util
    {
        static String TAG="ConsoleApp1";
        public static System.Collections.Specialized.StringDictionary parseCommandLines(String [] args)
        {
            System.Collections.Specialized.StringDictionary ret = new System.Collections.Specialized.StringDictionary();
            foreach(string s in args)
            {
                if (s.StartsWith('-'))
                {
                    string k = s.Substring(1);
                    string v = "";
                    int pos = s.IndexOf('=');
                    if (pos > 0)
                    {
                        k = s.Substring(1, pos - 1);
                        if (pos + 1 < s.Length)
                            v = s.Substring(pos + 1);
                    }
                    ret[k] = v;
                }
            }
            return ret; 
        }
        public static void logIt(String msg)
        {
            System.Diagnostics.Trace.WriteLine(string.Format("[{0}]: {1}", TAG, msg));
        }
    }
}
