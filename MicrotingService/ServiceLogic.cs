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
using System.Text;
using System.Threading.Tasks;

namespace MicrotingService
{
    public class ServiceLogic
    {
        #region var
        eFormCore.Core sdkCore;
        OutlookCore.Core outLook;
        Tools t = new Tools();
        string serviceLocation;
        #endregion

        //con
        public ServiceLogic()
        {
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

            serviceLocation = "c:\\microtingservice\\" + GetServiceName() + "\\";
            LogEvent("serviceLocation:'" + serviceLocation + "'");

            return serviceLocation;
        }

        protected String    GetServiceName()
        {
            // Calling System.ServiceProcess.ServiceBase::ServiceNamea allways returns
            // an empty string,
            // see https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387024

            // So we have to do some more work to find out our service name, this only works if
            // the process contains a single service, if there are more than one services hosted
            // in the process you will have to do something else

            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
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

        private void        LogEvent(string appendText)
        {
            try
            {
                File.AppendAllText(serviceLocation + "log\\log_" + DateTime.Now.ToString("MM.dd") + ".txt", DateTime.Now.ToString() + " // " + appendText + Environment.NewLine);
            }
            catch
            {
                //damn
            }
        }

        private void        LogException(string appendText)
        {
            try
            {
                File.AppendAllText(serviceLocation + "log\\FatalException_" + DateTime.Now.ToString("MM.dd_HH.mm.ss") + ".txt", DateTime.Now.ToString() + " // "+ appendText + Environment.NewLine);
            }
            catch
            {
                //damn
            }
        }
        #endregion
    }
}