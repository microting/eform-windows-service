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

                var installFolder = session.CustomActionData["INSTALLFOLDER"];

                var configurationExists = session.CustomActionData["CONFIGURATIONEXISTS"] == "1";
                var useExistingConfiguration = session.CustomActionData["USEEXISTINGCONFIGURATION"] == "1";
                if (configurationExists && useExistingConfiguration)
                    HandlePreviousConfigs(session, installFolder);
                else
                {
                    // create folders
                    Directory.CreateDirectory(installFolder + "log");
                    Directory.CreateDirectory(installFolder + "input");

                    var inputFolder = installFolder + "input";

                    // save connection strings
                    File.WriteAllText(inputFolder + "\\sql_connection_sdkCore.txt",
                        session.CustomActionData["CONNECTIONSTRING"].Replace("@@", ";"));
                    if (session.CustomActionData["OUTLOOKCONNECTIONSTRINGENABLED"] == "1")
                        File.WriteAllText(inputFolder + "\\sql_connection_outlook.txt",
                            session.CustomActionData["OUTLOOKCONNECTIONSTRING"].Replace("@@", ";"));
                }

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
                var instalUtilPath = "C:\\Windows\\Microsoft.NET\\Framework64\\v" + netVersion + "\\InstallUtil.exe";
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

                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));


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

                var keepSettings = session.CustomActionData["KEEPSETTINGS"] == "1";
                var keepFolders = keepSettings 
                    ? session.CustomActionData["KEEPFOLDERS"].Split(',') 
                    : new string[0];
                var keepFiles = keepSettings
                    ? session.CustomActionData["KEEPFILES"].Split(',')
                    : new string[0];

                DeleteDirectory(dir, keepFolders, keepFiles);

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

                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

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

        [CustomAction]
        public static ActionResult FormatConnectionStringsCA(Session session)
        {
            try
            {
                session["CONNECTIONSTRING"] = session["CONNECTIONSTRING"].Replace(";", "@@");
                if (session["OUTLOOKCONNECTIONSTRINGENABLED"] == "1")
                    session["OUTLOOKCONNECTIONSTRING"] = session["OUTLOOKCONNECTIONSTRING"].Replace(";", "@@");

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
        public static ActionResult TryFindConfigs(Session session)
        {
            try
            {
                var installFolder = session["INSTALLFOLDER"];
                var configFolders = session["KEEPFOLDERS"].Split(',');
                var configFiles = session["KEEPFILES"]== "{}" ? new string[0] : session["KEEPFILES"].Split(',');

                if (!Directory.Exists(installFolder))
                {
                    session["USEEXISTINGCONFIGURATION"] = null;
                    session["CONFIGURATIONEXISTS"] = null;
                    return ActionResult.Success;
                }

                var tmp = Path.Combine(Path.GetTempPath(), "MicrotingServiceTemp");
                Directory.CreateDirectory(tmp);
                Directory.CreateDirectory(Path.Combine(tmp, "files"));
                Directory.CreateDirectory(Path.Combine(tmp, "dirs"));

                var configFoldersFound = SaveConfigFolders(configFolders, tmp, installFolder);
                var configFilesFound = SaveConfigFiles(configFiles, tmp, installFolder);
                
                if (configFoldersFound || configFilesFound)
                {
                    session["CONFIGURATIONEXISTS"] = "1";
                    session["USEEXISTINGCONFIGURATION"] = "1";
                    return ActionResult.Success;
                }

                session["USEEXISTINGCONFIGURATION"] = null;
                session["CONFIGURATIONEXISTS"] = null;
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
                return ActionResult.Failure;
            }
        }

        private static void HandlePreviousConfigs(Session session, string installFolder)
        {
            var keepFiles = session.CustomActionData["KEEPFILES"].Split(',');
            var keepFolders = session.CustomActionData["KEEPFOLDERS"].Split(',');
            var tmpConfigs = Path.Combine(Path.GetTempPath(), "MicrotingServiceTemp");

            foreach (var keepFolder in keepFolders)
            {
                var path = Path.Combine(tmpConfigs, "dirs", keepFolder);
                if (Directory.Exists(path))
                    DirectoryCopy(path, Path.Combine(installFolder, keepFolder));
            }


            foreach (var keepFile in keepFiles)
            {
                var path = Path.Combine(tmpConfigs, "files", keepFile);
                if (File.Exists(path))
                    File.Copy(path, Path.Combine(installFolder, keepFile), true);
            }

            DeleteDirectory(tmpConfigs);
        }
        private static bool SaveConfigFiles(string[] configFiles, string tmp, string dir)
        {
            bool configsFound = false;
            foreach (var configFile in configFiles)
            {
                var configFileFullName = Path.Combine(dir, configFile);
                if (File.Exists(configFileFullName))
                {
                    configsFound = true;
                    var newDest = Path.Combine(tmp, "files", new FileInfo(configFileFullName).Name);
                    File.Copy(configFileFullName, newDest, true);
                }
            }

            return configsFound;
        }

        private static bool SaveConfigFolders(string[] configFolders, string tmp, string dir)
        {
            var configsFound = false;

            foreach (var configFolder in configFolders)
            {
                var configFolderFullName = Path.Combine(dir, configFolder);
                if (Directory.Exists(configFolderFullName))
                {
                    configsFound = true;
                    DirectoryCopy(configFolderFullName, Path.Combine(tmp, "dirs", new DirectoryInfo(configFolderFullName).Name));
                }
            }

            return configsFound;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool overrideFile = true)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                if (file.Name.Equals("Web.config", StringComparison.InvariantCultureIgnoreCase) && File.Exists(temppath))
                    continue;

                if (File.Exists(temppath) && !overrideFile)
                    continue;

                file.CopyTo(temppath, overrideFile);
            }

            //copying subdirectories, copy them and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }

        }

        public static void DeleteDirectory(string targetDir) =>
            DeleteDirectory(targetDir, new string[0], new string[0]);

        public static void DeleteDirectory(string targetDir, string[] keepFolders, string[] keepFiles) =>
            DeleteDirectory(targetDir, keepFolders, keepFiles, targetDir);

        public static void DeleteDirectory(string targetDir, string[] keepFolders, string[] keepFiles, string initialDir)
        {
            var keepFoldersModified = keepFolders.Select(t => Path.Combine(initialDir, t)).ToArray();
            var keepFilesModified = keepFiles.Select(t => Path.Combine(initialDir, t)).ToArray();

            var files = Directory.GetFiles(targetDir).Except(keepFilesModified);
            var dirs = Directory.GetDirectories(targetDir).Except(keepFoldersModified);

            foreach (string file in files)
            {
                File.SetAttributes(file, System.IO.FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
                DeleteDirectory(dir, keepFolders, keepFiles, initialDir);

            if (Directory.GetFiles(targetDir).Any() || Directory.GetDirectories(targetDir).Any())
                return;

            Directory.Delete(targetDir, false);
        }
    }
}
