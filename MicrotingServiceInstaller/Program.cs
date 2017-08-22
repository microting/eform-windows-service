/*
The MIT License (MIT)

Copyright (c) 2014 microting

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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
