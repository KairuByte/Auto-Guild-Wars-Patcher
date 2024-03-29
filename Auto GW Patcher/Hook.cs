﻿// Sample for CreateRemoteThread in C#
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
    class Program3
    {
        #region standard imports from kernel32

        // Thread proc, to be used with Create*Thread
        public delegate int ThreadProc(IntPtr param);

        // Friendly version, marshals thread-proc as friendly delegate
        [DllImport("kernel32")]
        public static extern IntPtr CreateThread(
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            ThreadProc lpStartAddress, // ThreadProc as friendly delegate
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint dwThreadId);

        // Marshal with ThreadProc's function pointer as a raw IntPtr.
        [DllImport("kernel32", EntryPoint = "CreateThread")]
        public static extern IntPtr CreateThreadRaw(
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress, // ThreadProc as raw IntPtr
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint dwThreadId);


        // CreateRemoteThread, since ThreadProc is in remote process, we must use a raw function-pointer.
        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          IntPtr lpStartAddress, // raw Pointer into remote process
          IntPtr lpParameter,
          uint dwCreationFlags,
          out uint lpThreadId
        );

        [DllImport("kernel32")]
        public static extern IntPtr GetCurrentProcess();

        const uint PROCESS_ALL_ACCESS = 0x000F0000 | 0x00100000 | 0xFFF;
        [DllImport("kernel32")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);



        [DllImport("kernel32")]
        public static extern
        uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        #endregion // standard imports from kernel32

        // stdcall        
        static int MyThreadProc(IntPtr param)
        {
            int pid = Process.GetCurrentProcess().Id;
            Application.Exit();
            MessageBox.Show("Pid " + pid + ": Inside my new thread!. Param=" + param.ToInt32());
            //Console.WriteLine("Pid {0}: Inside my new thread!. Param={1}", pid, param.ToInt32());
            return 1;
        }

        // Helper to wait for a thread to exit and print its exit code
        static void WaitForThreadToExit(IntPtr hThread)
        {
            WaitForSingleObject(hThread, unchecked((uint)-1));

            uint exitCode;
            GetExitCodeThread(hThread, out exitCode);
            int pid = Process.GetCurrentProcess().Id;
            MessageBox.Show("Pid " + pid + ": Thread exited with code: " + exitCode);
            //Console.WriteLine("Pid {0}: Thread exited with code: {1}", pid, exitCode);
        }

        // Main function 
        static void Mainly(string[] args)
        {
            int pid = Process.GetCurrentProcess().Id;
            if (args.Length == 0)
            {
                MessageBox.Show("Pid " + pid + ":Started Parent process");
                //Console.WriteLine("Pid {0}:Started Parent process", pid);

                // Spawn the child
                //string fileName = Process.GetCurrentProcess().MainModule.FileName.Replace(".vshost", "");
                string fileName = Directory.GetCurrentDirectory() + "\\gw.exe";

                // Get thread proc as an IntPtr, which we can then pass to the 2nd-process.
                ThreadProc proc = new ThreadProc(MyThreadProc);
                IntPtr fpProc = Marshal.GetFunctionPointerForDelegate(proc);

                // Spin up the other process, and pass our pid and function pointer so that it can
                // use that to call CreateRemoteThraed
                string arg = String.Format("{0} {1}", pid, fpProc);
                ProcessStartInfo info = new ProcessStartInfo(fileName, arg);
                info.UseShellExecute = false; // share console, output is interlaces.
                Process processChild = Process.Start(info);

                processChild.WaitForExit();
                return;
            }
            else
            {
                MessageBox.Show("Pid " + pid + ":Started Child process");
                //Console.WriteLine("Pid {0}:Started Child process", pid);
                uint pidParent = uint.Parse(args[0]);
                IntPtr fpProc = new IntPtr(int.Parse(args[1]));

                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pidParent);

                uint dwThreadId;
                // Create a thread in the first process.
                IntPtr hThread = CreateRemoteThread(
                    hProcess,
                    IntPtr.Zero,
                    0,
                    fpProc, new IntPtr(6789),
                    0,
                    out dwThreadId);
                WaitForThreadToExit(hThread);
                return;
            }
        }

        // Other  variations of create thread
        static void OtherMain(string[] args)
        {
            IntPtr fpProc = IntPtr.Zero;

            ThreadProc proc = new ThreadProc(MyThreadProc);
            fpProc = Marshal.GetFunctionPointerForDelegate(proc);

            uint dwThreadId;
#if false
            IntPtr hThread = CreateThreadRaw(
                IntPtr.Zero,
                0,
                fpProc, new IntPtr(1234),
                0, // flags
                out dwThreadId);
#elif false
            IntPtr hThread = CreateThread(
                IntPtr.Zero,
                0,
                proc, new IntPtr(1234),
                0, // flags
                out dwThreadId);
#else
            IntPtr hThisProcess = GetCurrentProcess();

            IntPtr hThread = CreateRemoteThread(
                hThisProcess,
                IntPtr.Zero,
                0,
                fpProc, new IntPtr(5678),
                0,
                out dwThreadId);
#endif
            WaitForThreadToExit(hThread);
        }
    }
}