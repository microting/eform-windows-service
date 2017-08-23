using MicrotingService;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string fakedServiceName = "MicrotingOdense";

            ServiceLogic serveiceLogic = new ServiceLogic();
            serveiceLogic.OverrideServiceLocation("c:\\microtingservice\\" + fakedServiceName + "\\");

            serveiceLogic.Start();
            Console.WriteLine("Press any key to close");
            Console.ReadKey();
            serveiceLogic.Stop();
        }
    }
}