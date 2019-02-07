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

using eFormShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;
using Microting.WindowsService.BasePn;

namespace MicrotingService
{
    public class ServiceLogic
    {
        #region var
        eFormCore.Core sdkCore;
        //OutlookCore.Core outLook;
        Tools t = new Tools();
        string serviceLocation;
        private string sSource;
        private string sLog;
        private CompositionContainer _container;

        [ImportMany]
        IEnumerable<Lazy<ISdkEventHandler>> eventHandlers;
        #endregion

        //con
        public ServiceLogic()
        {

            sSource = "MicrotingService";
            sLog = "Application";
            try
            {
                EventLog.CreateEventSource(sSource, "Application");
            }
            catch (SecurityException)
            {
                sSource = "Application";
            }
            catch (Exception e)
            {
                if (!e.HResult.Equals(-2147024809))
                    sSource = "Application";

                LogEvent(e.Message);
                try
                {
                    LogEvent(e.InnerException.Message);
                }
                catch { }

            }
            try
            {
                LogEvent("Service called");
                {
                    serviceLocation = "";
                    sdkCore = new eFormCore.Core();

                    //An aggregate catalog that combines multiple catalogs
                    var catalog = new AggregateCatalog();

                    //Adds all the parts found in the same assembly as the Program class
                    LogEvent("Start loading plugins...");
                    Console.WriteLine("Start loading plugins...");
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        string path = Path.Combine(GetServiceLocation(), @"Plugins");
                        Directory.CreateDirectory(path);
                        Console.WriteLine("Path for plugins is : " + path);
                        foreach (string dir in Directory.GetDirectories(path))
                        {
                            LogEvent("Loading Plugin : " + dir);
                            if (Directory.Exists(Path.Combine(dir, "netstandard2.0")))
                            {
                                Console.WriteLine("Loading Plugin : " + Path.Combine(dir, "netstandard2.0"));
                                catalog.Catalogs.Add(new DirectoryCatalog(Path.Combine(dir, "netstandard2.0")));
                            } else
                            {
                                Console.WriteLine("Loading Plugin : " + dir);
                                catalog.Catalogs.Add(new DirectoryCatalog(dir));
                            }
                            

                        }
                    } catch (Exception e) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Something went wrong in loading plugins: " + e.Message);
                        LogException("Something went wrong in loading plugins.");
                        LogException(e.Message);
                    }
                    //Create the CompositionContainer with the parts in the catalog
                    _container = new CompositionContainer(catalog);

                    //Fill the imports of this object
                    try
                    {
                        this._container.ComposeParts(this);
                    }
                    catch (CompositionException compositionException)
                    {
                        Console.WriteLine(compositionException.ToString());
                    }
                }
                LogEvent("Service completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
            }
        }

        #region public state
        public void Start()
        {

            string connectionString = File.ReadAllText(GetServiceLocation() + "input\\sql_connection_sdkCore.txt").Trim();
            Start(connectionString);
        }

        public void Start(string sdkSqlCoreStr)
        {
            #region start plugins
            try
            {

                foreach (Lazy<ISdkEventHandler> i in eventHandlers)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Trying to start plugin : " + i.Value.GetType().ToString());
                    LogEvent("Trying to start plugin : " + i.Value.GetType().ToString());
                    i.Value.Start(sdkSqlCoreStr, GetServiceLocation());
                    Console.WriteLine(i.Value.GetType().ToString() + " started successfully!");
                    LogEvent(i.Value.GetType().ToString() + " started successfully!");
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Start got exception : " + e.Message);
                LogException("Start got exception : " + e.Message);
            }
            #endregion

            try
            {
                LogEvent("Service Start called");
                {
                    // start debugger?
                    if (File.Exists(GetServiceLocation() + "input\\debug.txt"))
                    {
                        LogEvent("Debugger called");
                        System.Diagnostics.Debugger.Launch();
                    }

                    #region start SDK core
                    #region event connecting
                    try
                    {
                        sdkCore.HandleEventException -= CoreEventException;
                        sdkCore.HandleCaseRetrived += _caseRetrived;
                        sdkCore.HandleCaseCompleted += _caseCompleted;
                        sdkCore.HandleNotificationNotFound += _caseCompleted;
                        LogEvent("Core exception events disconnected (if needed)");
                    }
                    catch { }

                    sdkCore.HandleEventException += CoreEventException;
                    LogEvent("Core exception events connected");
                    #endregion

                    LogEvent("sdkSqlCoreStr, " + sdkSqlCoreStr);

                    sdkCore.Start(sdkSqlCoreStr);
                    LogEvent("SDK Core started");
                    #endregion                  
                }
                LogEvent("Service Start completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
				throw ex;
            }
        }

        public void Stop()
        {
            try
            {
                LogEvent("Service Close called");
                {
                    try
                    {

                        foreach (Lazy<ISdkEventHandler> i in eventHandlers)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Trying to stop plugin : " + i.Value.GetType().ToString());
                            i.Value.Stop(false);
                            Console.WriteLine(i.Value.GetType().ToString() + " stopped successfully!");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("Stop got exception : " + e.Message);
                    }
                    sdkCore.Close();
                    LogEvent("SDK Core closed");
                }
                LogEvent("Service Close completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
            }
        }

        public void OverrideServiceLocation(string serviceLocation)
        {
            this.serviceLocation = serviceLocation;
            LogEvent("serviceLocation:'" + serviceLocation + "'");
        }
        #endregion

        #region private
        private string GetServiceLocation()
        {
            if (serviceLocation != "")
                return serviceLocation;

            serviceLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                serviceLocation = Path.GetDirectoryName(serviceLocation) + "/";
            }
            else
            {
                serviceLocation = Path.GetDirectoryName(serviceLocation) + "\\";
            }
            
            LogEvent("serviceLocation:'" + serviceLocation + "'");

            return serviceLocation;
        }

        #region _caseRetrived
        private void _caseRetrived(object sender, EventArgs args)
        {
            try
            {

                foreach (Lazy<ISdkEventHandler> i in eventHandlers)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Trying to send event caseRetrieved to plugin : " + i.Value.GetType().ToString());
                    i.Value.eFormRetrived(sender, args);
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("_caseRetrived got exception : " + e.Message);
            }
        }
        #endregion

        #region _caseCreated
        private void _caseCompleted(object sender, EventArgs args)
        {
			//Console.ForegroundColor = ConsoleColor.Green;
			//Console.WriteLine($"_caseCompleted called ");
   //         Case_Dto trigger = (Case_Dto)sender;
   //         int siteId = trigger.SiteUId;
   //         string caseType = trigger.CaseType;
   //         string caseUid = trigger.CaseUId;
   //         string mUId = trigger.MicrotingUId;
   //         string checkUId = trigger.CheckUId;
   //         string CaseId = trigger.CaseId.ToString();
   //         LogEvent("_caseCompleted siteId is :'" + siteId + "'");
   //         LogEvent("_caseCompleted caseType is :'" + caseType + "'");
   //         LogEvent("_caseCompleted caseUid is :'" + caseUid + "'");
   //         LogEvent("_caseCompleted mUId is :'" + mUId + "'");
   //         LogEvent("_caseCompleted checkUId is :'" + checkUId + "'");
   //         LogEvent("_caseCompleted CaseId is :'" + CaseId + "'");

            try
            {

                foreach (Lazy<ISdkEventHandler> i in eventHandlers)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Trying to send event _caseCompleted to plugin : " + i.Value.GetType().ToString());
                    i.Value.CaseCompleted(sender, args);
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("_caseCompleted got exception : " + e.Message);
            }

        }
        #endregion

        #region _caseNotFound
        private void _caseNoFound(object sender, EventArgs args)
        {

            Note_Dto trigger = (Note_Dto)sender;            
        }
        #endregion

        protected String GetServiceName()
        {
            // Calling System.ServiceProcess.ServiceBase::ServiceNamea allways returns
            // an empty string,
            // see https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387024

            // So we have to do some more work to find out our service name, this only works if
            // the process contains a single service, if there are more than one services hosted
            // in the process you will have to do something else

            int processId = Process.GetCurrentProcess().Id;
            String query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            System.Management.ManagementObjectSearcher searcher =
                new System.Management.ManagementObjectSearcher(query);

            foreach (System.Management.ManagementObject queryObj in searcher.Get())
            {
                return queryObj["Name"].ToString();
            }

            throw new Exception("Can not get the ServiceName");
        }

        private void CoreEventException(object sender, EventArgs args)
        {
            //DOSOMETHING: changed to fit your wishes and needs 
            Exception ex = (Exception)sender;
        }

        private void LogEvent(string appendText)
        {
            try
            {
                EventLog.WriteEntry(sSource, appendText, EventLogEntryType.Information);
            }
            catch
            { }
        }

        private void LogException(string appendText)
        {
            try
            {
                EventLog.WriteEntry(sSource, appendText, EventLogEntryType.Error);
            }
            catch
            {

            }
        }
        #endregion
    }
}