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
            string serverConnectionString = "";
            //string fakedServiceName = "MicrotingOdense";
            Console.WriteLine("Enter database to use:");
            Console.WriteLine("> If left blank, it will use 'Microting'");
            Console.WriteLine("  Enter name of database to be used");
            string databaseName = Console.ReadLine();

            if (databaseName.ToUpper() != "")
                serverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=" + databaseName + ";Integrated Security=True";
            if (databaseName.ToUpper() == "T")
                serverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=" + "MicrotingTest" + ";Integrated Security=True";
            if (databaseName.ToUpper() == "O")
                serverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=" + "MicrotingOdense" + ";Integrated Security=True";
            if (serverConnectionString == "")
                serverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=" + "MicrotingSourceCode" + ";Integrated Security=True";

            ServiceLogic serveiceLogic = new ServiceLogic();
            //serveiceLogic.OverrideServiceLocation("c:\\microtingservice\\" + fakedServiceName + "\\");

            serveiceLogic.Start(serverConnectionString);
            Console.WriteLine("Press any key to close");
            Console.ReadKey();
            serveiceLogic.Stop();
        }
    }
}