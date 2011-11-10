using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Auto_GW_Patcher
{
    static class Program
    {
        public const string GW_PROCESS_NAME = "Gw";
        public const string GW_FILENAME = "Gw.exe";
        public const string GW_DAT = "Gw.dat";
        public const string MUTEX_MATCH_STRING      = "AN-Mute";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
