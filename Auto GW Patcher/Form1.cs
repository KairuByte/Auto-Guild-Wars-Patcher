using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using System.Threading;



namespace Auto_GW_Patcher
{
    public partial class Form1 : Form
    {
        ManagementEventWatcher watcher;

        private bool allowshowdisplay = false;

        private int detectDelay = 1;
        private bool exit = false;
        private int retries = 100;

        private bool alertMe = true;

        private bool needsRelease = false;

        public Form1()
        {
            WatchForProcessStart("gw.exe");
            WatchForProcessEnd("gw.exe");
            watcher.Start();
            InitializeComponent();
        }

        private void cmdPatch_Click(object sender, EventArgs e)
        {
            if (!autoToolStripMenuItem.Checked)
            {
                watcher.Start();
                autoToolStripMenuItem.Checked = true;
            }
            else
            {
                watcher.Stop();
                autoToolStripMenuItem.Checked = false;
            }
        }

        public ManagementEventWatcher WatchForProcessStart(string processName)
        {
            string queryString =
                "SELECT TargetInstance" +
                "  FROM __InstanceCreationEvent " +
                "WITHIN  "  + 1 + " " +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + processName + "'";

            // The dot in the scope means use the current machine
            string scope = @"\\.\root\CIMV2";

            // Create a watcher and listen for events
            watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += ProcessStarted;
            return watcher;
        }

        public ManagementEventWatcher WatchForProcessEnd(string processName)
        {
            string queryString =
                "SELECT TargetInstance" +
                "  FROM __InstanceDeletionEvent " +
                "WITHIN  1 " +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + processName + "'";

            // The dot in the scope means use the current machine
            string scope = @"\\.\root\CIMV2";

            // Create a watcher and listen for events
            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += ProcessEnded;
            watcher.Start();
            return watcher;
        }
        public FileSystemWatcher WatchForDatWrite(string processName)
        {
            FileSystemWatcher fswatcher = new FileSystemWatcher();
            fswatcher.Path = Directory.GetCurrentDirectory();
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            fswatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            fswatcher.Filter = "gw.dat";

            // Add event handlers.
            fswatcher.Changed += new FileSystemEventHandler(DatChanged);

            // Begin watching.
            fswatcher.EnableRaisingEvents = true;
            return fswatcher;
        }

        public void ProcessEnded(object sender, EventArrivedEventArgs e)
        {
            notifyIcon1.BalloonTipText = "Guild Wars Closed!";
            if (needsRelease)
                needsRelease = true;
            if (alertMe)
                notifyIcon1.ShowBalloonTip(1000);
        }

        public void ProcessStarted(object sender, EventArrivedEventArgs e)
        {

            notifyIcon1.BalloonTipText = "New Guild Wars Detected: \n";
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;

            for (int i = 0; i < retries; i++)
            {
                if (HandleManager.KillHandle(Process.GetProcessById(Convert.ToInt32(targetInstance.Properties["ProcessID"].Value.ToString())), Program.MUTEX_MATCH_STRING, false))
                {
                    notifyIcon1.BalloonTipText += "Mutex Removed!";
                    break;
                }
                else if (i < retries - 1)
                    Thread.Sleep(5);
                else
                    notifyIcon1.BalloonTipText += "Mutex Error!";
            }
            needsRelease = false;
            if (needsRelease)
                if (HandleManager.ClearDatLock(Directory.GetCurrentDirectory()))
                    notifyIcon1.BalloonTipText += "DAT Unlocked!";
                else
                    notifyIcon1.BalloonTipText += "DAT Error!";
            else
                needsRelease = true;
            if(alertMe)
                notifyIcon1.ShowBalloonTip(1000);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtDetectDelay.Text = detectDelay.ToString();
            toolTip1.SetToolTip(txtDetectDelay, "Lower numbers detect the game faster, but may cause problems. If you get a lot of errors, raise the number.\nRecommended: 10");
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            if(!exit)
                e.Cancel = true;
        }
        public void ExitApp()
        {
            Application.Exit();
        }
        protected void Exit_Click(Object sender, System.EventArgs e)
        {
            exit = true;
            ExitApp();
        }
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
        private void cmdAccept_Click(object sender, EventArgs e)
        {
            detectDelay = Convert.ToInt32(txtDetectDelay.Text);
            this.Hide();
        }
        private void Hide()
        {
            allowshowdisplay = false;
            this.Visible = false;
        }
        private void Show()
        {
            allowshowdisplay = true;
            this.Visible = true;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            this.Show();

        }
        public void DatChanged(object source, FileSystemEventArgs e)
        {
            MessageBox.Show("Test");
        }

        private void chkAlertMe_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAlertMe.Checked)
                alertMe = true;
            else
                alertMe = false;
        }
    }
}
