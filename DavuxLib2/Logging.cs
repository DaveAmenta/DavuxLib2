using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DavuxLib2
{
    public class Logging
    {
        public static string FileName = "log.txt";
        public static string CrashPrefix = "LoggedCrash_";

        internal static void Init()
        {
            Trace.AutoFlush = true;

            if (!Debugger.IsAttached)
            {
                // Don't remove the Tracer for the VS Output window
                Trace.Listeners.Clear();
                Trace.Listeners.Add(new ConsoleTraceListener(false));
            }

            FileStream fs = new FileStream(Path.Combine(App.DataDirectory, FileName), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            Trace.Listeners.Add(new TextWriterTraceListenerEx(fs));

            Trace.WriteLine(GenerateInfo());

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    File.WriteAllText(CrashFile, GenerateInfo() + e.ExceptionObject.ToString());
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("DavuxLib2/Logging/Crash: Unable to write file: " + ex);
                }
                Trace.Fail("Last-Chance Exception: " + e.ExceptionObject);
            };
        }

        internal static string GenerateInfo()
        {
            string ret = "";
            ret += "Logging Started for " + App.Name + " at " + DateTime.Now;
            ret += Environment.NewLine + "Using: " + Environment.OSVersion.VersionString + " on " + Environment.MachineName;
            ret += Environment.NewLine + "Executing From: " + Environment.CurrentDirectory;
            ret += Environment.NewLine + "Data Directory: " + App.DataDirectory;
            ret += Environment.NewLine + Assembly.GetExecutingAssembly().FullName;
            ret += Environment.NewLine + Assembly.GetEntryAssembly().FullName + Environment.NewLine;
            return ret;
        }

        internal static string CrashFile
        {
            get
            {
                return Path.Combine(App.DataDirectory, CrashPrefix + App.SessionID + ".txt");
            }
        }

        internal static string FullPath
        {
            get { return Path.Combine(App.DataDirectory, FileName); }
        }
    }

    class TextWriterTraceListenerEx : TextWriterTraceListener
    {
        public override void Write(string message)
        {
            base.Write(DateTime.Now.ToLongTimeString() + ": " + message);
        }

        public override void WriteLine(string message)
        {
            base.WriteLine(DateTime.Now.ToLongTimeString() + ": " + message);
        }

        public override void Fail(string message)
        {
            base.WriteLine("FATAL: " + DateTime.Now.ToLongTimeString() + ": " + message);
            base.Fail(message);
        }

        public TextWriterTraceListenerEx(FileStream fs)
            : base(fs)
        { }
    }
}
