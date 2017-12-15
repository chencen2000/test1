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

        public static System.Tuple<Boolean, int, String[]> runExe(String cmd, String args, int timeout=60)
        {
            System.Tuple<Boolean, int, String[]> ret=new System.Tuple<Boolean, int ,String[]>(false,-1,null);
            logIt(String.Format("runExe: ++ cmd={0} args={1}", cmd, args));
            try
            {
                int exit_code=-1;
                System.Threading.AutoResetEvent ev = new System.Threading.AutoResetEvent(false);
                List<String> lines=new List<String>();
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = cmd;
                p.StartInfo.Arguments=args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.OutputDataReceived += (obj, e) =>
                {
                    if(!String.IsNullOrEmpty(e.Data))
                    {
                        logIt(e.Data);
                        lines.Add(e.Data);
                    }
                    if(e.Data==null)
                    {
                        // pipe closed
                        ev.Set();
                    }
                };
                p.Start();
                p.BeginOutputReadLine();
                if(p.WaitForExit(timeout))
                {
                    exit_code=p.ExitCode;
                    ev.WaitOne(2500);
                }
                else
                {
                    if(!p.HasExited)
                        p.Kill();
                    exit_code=1460;
                }
                ret=new System.Tuple<Boolean,int,String[]>(true,exit_code,lines.ToArray());                
            }
            catch(System.Exception ex)
            {
                logIt(ex.Message);
                logIt(ex.StackTrace);
            }            
            logIt(String.Format("runExe: -- ret={0} code={1}", ret.Item1, ret.Item2));
            return ret;
        }
    }
}
