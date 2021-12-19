using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MockConsoleApp
{
    public enum LogLevel
    {
        FTL,
        FATAL,
        Ftl,
        Fatal,
        ftl,
        fatal,

        ERR,
        ERROR,
        Err,
        Error,
        err,
        error,

        WARN,
        WARNING,
        Warn,
        Warning,
        warn,
        warning,

        INFO,
        INFORMATION,
        Info,
        Information,
        info,
        information,

        DBG,
        DEBUG,
        Dbg,
        Debug,
        dbg,
        debug,
    }

    public class Logger
    {
        // \[(.+?)\] \[(.+?)\] \[(.+?)\] (.+) (.+?)\(([\d]+)\)
        // [DateTime] [Level] [Thread] Text File(Line)
        public static void Log(LogLevel level, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var now = DateTime.Now;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"[{now}] [{level}] [{threadId}] {message}");
            Debug.WriteLine($"[{now}] [{level}] [{threadId}] {message} {Path.GetFileName(file)}({line})");
        }
    }
}
