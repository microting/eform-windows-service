using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace MicrotingCustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult InstalCA(Session session)
        {
            try
            {
                if (session.CustomActionData["INSTMODE"] != "Install")
                    return ActionResult.Success;

                // create folders
                var installFolder = session.CustomActionData["INSTALLFOLDER"];
                Directory.CreateDirectory(installFolder + "log");
                Directory.CreateDirectory(installFolder + "input");

                var inputFolder = installFolder + "input";

                // save connection strings
                File.WriteAllText(inputFolder + "\\sql_connection_sdkCore.txt",
                    session.CustomActionData["CONNECTIONSTRING"]);
                if (session.CustomActionData["OUTLOOKCONNECTIONSTRINGENABLED"] == "1")
                    File.WriteAllText(inputFolder + "\\sql_connection_outlook.txt",
                        session.CustomActionData["OUTLOOKCONNECTIONSTRING"]);

                // save products list into registry
                var serviceName = session.CustomActionData["SERVICENAME"];
                var vendorName = "Microting";
                var soft = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                if (!soft.GetSubKeyNames().Contains(vendorName))
                    soft.CreateSubKey(vendorName);

                var vendor = soft.OpenSubKey(vendorName, true);
                var services = vendor?.GetValue("Services")?.ToString();
                var list = new List<string> {serviceName};
                if (!string.IsNullOrEmpty(services))
                    list.Add(services);
                if (services != null && !services.Contains(serviceName))
                    vendor.SetValue("Services", string.Join(",", list.ToArray()));
                else
                    vendor.SetValue("Services", serviceName);

                // install service
                var netVersion = Environment.Version.ToString(3);
                var instalUtilPath = "C:\\Windows\\Microsoft.NET\\Framework\\v" + netVersion + "\\InstallUtil.exe";
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = instalUtilPath,
                        Arguments = $"/servicename=\"{serviceName}\" \"{installFolder}MicrotingService.exe\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();

                //var str = string.Empty;
                //while (!proc.StandardOutput.EndOfStream)
                //{
                //    str+=proc.StandardOutput.ReadLine();
                //}
                //MessageBox.Show(str);
                while (!proc.HasExited)
                    Thread.Sleep(500);

                // start service
                var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Running)
                    service.Start();

                service.WaitForStatus(ServiceControllerStatus.Running);
                
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
                session.Log(ex.Message);
                session.Log(ex.StackTrace);
                return ActionResult.Failure;
            }

        }

        [CustomAction]
        public static ActionResult GetServicesListCA(Session session)
        {
            try
            {
                var vendorName = "Microting";
                var soft = Registry.Users.OpenSubKey(".default\\SOFTWARE");
                var vendor = soft.OpenSubKey(vendorName);
                if (vendor == null)
                {
                    session["NOSERVICES"] = "1";
                    return ActionResult.Success;
                }

                var services = vendor?.GetValue("Services")?.ToString();
                if (string.IsNullOrEmpty(services))
                {
                    session["NOSERVICES"] = "1";
                    return ActionResult.Success;
                }

                var clearDefault = session.Database.OpenView("DELETE FROM ComboBox WHERE ComboBox.Property='SERVICENAME'");
                clearDefault.Execute();

                var lView = session.Database.OpenView("SELECT * FROM ComboBox WHERE ComboBox.Property='SERVICENAME'");
                lView.Execute();

                var list = services.Split(',');
                int index = 1;
                foreach (var service in list)
                {
                    Record lRecord = session.Database.CreateRecord(4);
                    lRecord.SetString(1, "SERVICENAME");
                    lRecord.SetInteger(2, index);
                    lRecord.SetString(3, service);
                    lRecord.SetString(4, service);
                    lView.Modify(ViewModifyMode.InsertTemporary, lRecord);

                    ++index;
                }

                lView.Close();

                session["SERVICENAME"] = list.First();

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult UninstallServiceCA(Session session)
        {
            try
            {  
                if (session.CustomActionData["INSTMODE"] != "Remove")
                    return ActionResult.Success;

                var serviceName = session.CustomActionData["SERVICENAME"];

                // stop service
                var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Stopped)
                    service.Stop();

                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));


                // uninstall service
                var regkey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceName}");

                var servicePath = regkey.GetValue("ImagePath").ToString(); 

                var netVersion = Environment.Version.ToString(3);
                var instalUtilPath = "C:\\Windows\\Microsoft.NET\\Framework\\v" + netVersion + "\\InstallUtil.exe";
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = instalUtilPath,
                        Arguments = $"/servicename=\"{serviceName}\" /u {servicePath}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                while (!proc.HasExited)
                    Thread.Sleep(500);

                // remove service from registry
                var vendorKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microting", true);
                var services = vendorKey.GetValue("Services")?.ToString().Split(',');
                vendorKey.SetValue("Services", string.Join(",", services.Where(t => t != serviceName).ToArray()));

                // remove service folder
                var dir = Path.GetDirectoryName(servicePath.Replace("\"", ""));
                var directoryInfo = new DirectoryInfo(dir);
                directoryInfo.Delete(true);


                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult UpdateServiceCA(Session session)
        {
            try
            {
                if (session.CustomActionData["INSTMODE"] != "Update")
                    return ActionResult.Success;

                var serviceName = session.CustomActionData["SERVICENAME"];

                // start service
                var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Running)
                    service.Start();

                service.WaitForStatus(ServiceControllerStatus.Running);

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult StopBeforUpdateServiceCA(Session session)
        {
            try
            {
                if (session.CustomActionData["INSTMODE"] != "Update")
                    return ActionResult.Success;

                var serviceName = session.CustomActionData["SERVICENAME"];
                
                // start service
                var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Stopped)
                    service.Stop();

                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));

                var regkey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{serviceName}");

                var servicePath = regkey.GetValue("ImagePath").ToString();
                var dir = Path.GetDirectoryName(servicePath.Replace("\"", ""));
                var directoryInfo = new DirectoryInfo(dir);
                var fileInfos = directoryInfo.GetFiles();
                fileInfos.ToList().ForEach(f => f.Delete());

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
                return ActionResult.Failure;
            }
        }
    }
}
