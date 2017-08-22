using System;
using System.IO;
using System.ServiceProcess;

namespace MicrotingService
{
    partial class Service
    {
        #region 'var'
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        System.ComponentModel.IContainer components = null;
        ServiceLogic logic;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region 'con'
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            logic = new ServiceLogic();
        }
        #endregion

        protected override void OnStart(string[] args)
        {
            logic.Start();
        }

        protected override void OnStop()
        {
            logic.Stop();
        }
    }
}