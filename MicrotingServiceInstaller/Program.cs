using eFormCore;
using OutlookSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicrotingServiceInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("Enter SDK's database connection string:");
            string sdkCon = Console.ReadLine();

            Console.WriteLine("Processing...");
            AdminTools at = new AdminTools(sdkCon);
            Console.WriteLine("Completed...");

            Console.WriteLine("");
            Console.WriteLine("Enter token:");
            string token = Console.ReadLine();

            Console.WriteLine("Processing...");
            at.DbSetup(token);
            Console.WriteLine("Completed...");

            Console.WriteLine("");
            Console.WriteLine("Enter Outlook's database connection string:");
            string outlookCon = Console.ReadLine();

            Console.WriteLine("Processing...");
            SqlController outlook = new SqlController(outlookCon);
            outlook.SettingUpdate(Settings.microtingDb, sdkCon);
            Console.WriteLine("Completed...");

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("!!!  Please check, and change where needed, the setting tables in the SDK and Outlook databases  !!!");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("(press any key to close)");
            Console.ReadLine();

            Console.WriteLine("");
            Console.WriteLine("Closing app in a sec");
            Thread.Sleep(1000);
        }
    }
}
