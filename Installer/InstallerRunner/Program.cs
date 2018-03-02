using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace InstallerRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var install =
                    Registry.ClassesRoot.OpenSubKey(@"Installer\Products",
                        true);

                var products = install.GetSubKeyNames().Where(t =>
                    install.OpenSubKey(t).GetValue("ProductName").ToString().Contains("Microting Windows Service"));

                if (products != null)
                    foreach (var product in products)
                        install.OpenSubKey(product, true).SetValue("PackageCode", "");

                string path = Path.Combine(Path.GetTempPath(), "Microting Windows Service.msi");
                File.WriteAllBytes(path, Resource.MicrotingServiceInstaller);
                Process.Start(path);
            }
            catch (SecurityException e)
            {
                Console.WriteLine("Please run installer package as administrator");
            }

        }
    }
}
