using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockConsoleApp
{
    public class Class1
    {
        public static void DoSomthing()
        {
            Logger.Log(LogLevel.Warning, "DoSomthing-Warn");
            Logger.Log(LogLevel.Error, "DoSomthing-Error");
        }
    }
}
