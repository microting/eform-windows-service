using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                install.OpenSubKey(t).GetValue("ProductName")?.ToString().Contains("Microting Windows Service") == true);

                if (products != null)
                    foreach (var product in products)
                        install.OpenSubKey(product, true).SetValue("PackageCode", "");

                var drive = DriveInfo.GetDrives().First(t => t.DriveType == DriveType.Fixed).Name;
                var tmpDir = Path.Combine(drive, "tmp");
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);

                var dirInfo = Directory.CreateDirectory(tmpDir);
                dirInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

                string path = Path.Combine(tmpDir, "Microting Windows Service.msi");
                File.WriteAllBytes(path, Resource.MicrotingServiceInstaller);
                Process.Start(path);
            }
            catch (SecurityException e)
            {
                MessageBox.Show("Please run installer package as administrator");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " " + e.StackTrace);
            }
        }
    }
}
