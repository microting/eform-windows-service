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
using System.Text;
using System.Threading.Tasks;
using System.Security;

namespace MicrotingService
{
    public class ServiceLogic
    {
        #region var
        eFormCore.Core sdkCore;
        OutlookCore.Core outLook;
        Tools t = new Tools();
        string serviceLocation;
        private FileSystemWatcher _fileWatcher;
        private List<string> fileNames;
        private string inboundPath;
        string sSource;
        string sLog;
        string sEvent;

        // DEPRECATED REMOVED IN A NEW VERSION
        bool fileHandlingEnabled = false;
        // DEPRECATED REMOVED IN A NEW VERSION
        #endregion

        //con
        public ServiceLogic()
        {

            sSource = "MicrotingService";
            sLog = "Application";
            bool sourceExists;
            try
            {
                EventLog.CreateEventSource(sSource, "Application");
            }
            catch (SecurityException)
            {
                sSource = "Application";
            }
            try
            {
                LogEvent("Service called");
                {
                    serviceLocation = "";
                    sdkCore = new eFormCore.Core();
                    outLook = new OutlookCore.Core();
                }
                LogEvent("Service completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
            }
        }

        #region public state
        public void         Start()
        {
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
                        sdkCore.HandleCaseCompleted += _caseCompleted;
                        LogEvent("Core exception events disconnected (if needed)");
                    }
                    catch { }
                   
                    sdkCore.HandleEventException += CoreEventException;
                    LogEvent("Core exception events connected");
                    #endregion

                    string sdkSqlCoreStr = File.ReadAllText(GetServiceLocation() + "input\\sql_connection_sdkCore.txt").Trim();
                    LogEvent("sdkSqlCoreStr, " + sdkSqlCoreStr);

                    sdkCore.Start(sdkSqlCoreStr);
                    LogEvent("SDK Core started");
                    #endregion

                    #region start Outlook
                    if (File.Exists(GetServiceLocation() + "input\\sql_connection_outLook.txt"))
                    {
                        string outlookSqlStr = File.ReadAllText(GetServiceLocation() + "input\\sql_connection_outLook.txt").Trim();
                        LogEvent("outlookSqlStr, " + outlookSqlStr);

                        outLook.Start(outlookSqlStr);
                        LogEvent("Outlook started");
                    }
                    #endregion

                    #region start FileWatcher 
                    // DEPRECATION WARNING!!! THIS WILL BE REMOVED IN A LATER VERSION
                    if (File.Exists(GetServiceLocation() + "input\\inboundPath.txt"))
                    {
                        inboundPath = File.ReadAllText(GetServiceLocation() + "input\\inboundPath.txt").Trim();
                        _fileWatcher = new FileSystemWatcher(inboundPath);

                        _fileWatcher.Created += _fileWatcher_Created;

                        _fileWatcher.EnableRaisingEvents = true;

                        fileNames = new List<string>();
                        fileHandlingEnabled = true;
                        LogEvent("Filewatecher started");
                    }
                    // DEPRECATION WARNING!!! THIS WILL BE REMOVED IN A LATER VERSION
                    #endregion
                }
                LogEvent("Service Start completed");
            }
            catch (Exception ex)
            {
                LogException(t.PrintException("Fatal Exception", ex));
            }
        }

        public void         Stop()
        {
            try
            {
                LogEvent("Service Close called");
                {
                    outLook.Close();
                    LogEvent("Outlook closed");

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

        public void         OverrideServiceLocation(string serviceLocation)
        {
            this.serviceLocation = serviceLocation;
            LogEvent("serviceLocation:'" + serviceLocation + "'");
        }
        #endregion

        #region private
        private string      GetServiceLocation()
        {
            if (serviceLocation != "")
                return serviceLocation;

            serviceLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            serviceLocation = Path.GetDirectoryName(serviceLocation) + "\\";
            LogEvent("serviceLocation:'" + serviceLocation + "'");

            return serviceLocation;
        }

        #region _fileWather_Created
        // DEPRECATION WARNING!!! THIS WILL BE REMOVED IN A LATER VERSION
        private void _fileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (!fileNames.Contains(e.Name))
            {
                LogEvent("_fileWatcher_Created called for file :" + e.Name);
                fileNames.Add(e.Name);
                char[] delimiter = { '_' };
                int siteId = int.Parse(e.Name.Split(delimiter)[0]);
                string navId = e.Name.Split(delimiter)[1].Replace(".xml", "");
                string rawXml = "";
                foreach ( string line in File.ReadLines(e.FullPath)) {
                    rawXml += line;
                }
                eFormData.MainElement mainElement = sdkCore.TemplateFromXml(rawXml);
                int template_id = sdkCore.TemplateCreate(mainElement);

                // We read the MainElement from db, since we need our own ids.
                mainElement = sdkCore.TemplateRead(template_id);

                List<int> siteIds = new List<int>();
                siteIds.Add(siteId);
                List<string> mtUid = sdkCore.CaseCreate(mainElement, "", siteIds, navId);
                string newEnding = "_" + mtUid.First().ToString() + ".xml";
                string newFileName = e.FullPath.Replace("inbound", "parsed").Replace(".xml", newEnding);
                File.Move(e.FullPath, newFileName);
                LogEvent(String.Format("File created: Path {0}, Name: {1}", e.FullPath, e.Name));
            }
        }
        // DEPRECATION WARNING!!! THIS WILL BE REMOVED IN A LATER VERSION
        #endregion

        #region _caseCreated
        // DEPRECATION WARNING!!! THIS WILL BE REMOVED IN A LATER VERSION
        private void _caseCompleted(object sender, EventArgs args)
        {
            if (fileHandlingEnabled)
            {

                LogEvent(String.Format("_caseCompleted called"));
                LogEvent(String.Format("_caseCompleted called inboundPath is : " + inboundPath));
                Case_Dto trigger = (Case_Dto)sender;
                int siteId = trigger.SiteUId;
                string caseType = trigger.CaseType;
                string caseUid = trigger.CaseUId;
                string mUId = trigger.MicrotingUId;
                string checkUId = trigger.CheckUId;

                string nav_id = trigger.Custom;

                string oldPath = inboundPath.Replace("inbound", "parsed") + "\\" + siteId.ToString() + "_" + nav_id + "_" + mUId + ".xml";
                string newPath = inboundPath.Replace("inbound", "outbound") + "\\" + siteId.ToString() + "_" + nav_id + "_" + mUId + ".xml";
                LogEvent(String.Format("_caseCompleted called oldPath is : " + oldPath));

                File.Move(oldPath, newPath);
                LogEvent(String.Format("_caseCompleted completed for file " + oldPath));
            }
        }
        // DEPRECATION WARNING!!! THIS WILL BE REMOVED IN A LATER VERSION
        #endregion

        protected String    GetServiceName()
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

        private void        CoreEventException(object sender, EventArgs args)
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

        private void        LogException(string appendText)
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