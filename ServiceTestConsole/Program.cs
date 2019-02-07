using MicrotingService;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (databaseName.ToUpper() != "")
                    serverConnectionString = @"Server = localhost; port = 3306; Database = " + databaseName +
                                             "; user = root; Convert Zero Datetime = true;";
                if (databaseName.ToUpper() == "T")
                    serverConnectionString =
                        @"Server=localhost;port=3306;Database=MicrotingTest;user=root;Convert Zero Datetime=true;";
                if (databaseName.ToUpper() == "O")
                    serverConnectionString =
                        @"Server=localhost;port=3306;Database=MicrotingOdense;user=root;Convert Zero Datetime=true;";
                if (serverConnectionString == "")
                    serverConnectionString =
                        @"Server=localhost;port=3306;Database=420_SDK;user=root;Convert Zero Datetime=true;";
            }
            else
            {
                if (databaseName.ToUpper() != "")
                    serverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=" + databaseName +
                                             ";Integrated Security=True";
                if (databaseName.ToUpper() == "T")
                    serverConnectionString =
                        @"Data Source=.\SQLEXPRESS;Initial Catalog=MicrotingTest;Integrated Security=True";
                if (databaseName.ToUpper() == "O")
                    serverConnectionString =
                        @"Data Source=.\SQLEXPRESS;Initial Catalog=MicrotingOdense;Integrated Security=True";
                if (serverConnectionString == "")
                    serverConnectionString =
                        @"Data Source=.\SQLEXPRESS;Initial Catalog=420_SDK;Integrated Security=True";
            }
            

            ServiceLogic serveiceLogic = new ServiceLogic();
            //serveiceLogic.OverrideServiceLocation("c:\\microtingservice\\" + fakedServiceName + "\\");

            serveiceLogic.Start(serverConnectionString);
            Console.WriteLine("Press any key to close");
            Console.ReadKey();
            serveiceLogic.Stop();
        }        
    }
}