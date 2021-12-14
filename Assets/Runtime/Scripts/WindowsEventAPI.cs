// Author : Shinn
// Date : 20191007
// Only for windows.
// https://docs.microsoft.com/zh-tw/dotnet/api/system.diagnostics.processwindowstyle?view=netframework-4.8
// 

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ScreenRecorder
{
    public static class WindowsEventAPI
    {
        #region API
        /// <summary>
        /// WindowsStyle
        /// </summary>
        public enum WindowsStyle
        {
            Hidden,
            Maximized,
            Minimized,
            Normal
        }

        /// <summary>
        /// 開啟 Windows 檔案
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        public static void OpenExetnationFile(string path, string fileName)
        {
            string file = Path.Combine(path, fileName);
            Process.Start(file);
        }

        /// <summary>
        /// 搜尋目前正在執行的程式
        /// </summary>
        public static void SearchAllProgramsOnRunning()
        {
            Process[] p1 = Process.GetProcesses();
            foreach (Process pro in p1)
                UnityEngine.Debug.Log(pro.ProcessName);
        }

        /// <summary>
        /// 尋找目前正在運行的 processName 程式
        /// </summary>
        /// <param name="processName"></param>
        public static void SearchProgram()
        {
            Process[] p1 = Process.GetProcesses();
            foreach (Process pro in p1)
                UnityEngine.Debug.Log(pro.ProcessName);
        }

        /// <summary>
        /// 可自定 Windows 的動作, 像是 隱藏 一般 最大化 最小化, 此程式會開啟程式, 不須額外執行 Process.Start()
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        /// <param name="windowStyle"></param>
        /// 
        /// Example:相對路徑
        /// string path = System.IO.Path.Combine(Environment.CurrentDirectory, @"..\exe");
        /// WindowsEventAPI.SetWindowEvent(path, "Kinect.exe", ProcessWindowStyle.Minimized);
        public static void SetWindowEvent(string path, string fileName, WindowsStyle windowStyle = WindowsStyle.Normal)
        {
            string file = Path.Combine(path, fileName);
            using (Process myProcess = new Process())
            {
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = file;
                myProcess.StartInfo.CreateNoWindow = false;

                myProcess.StartInfo.WindowStyle = Select(windowStyle);
                myProcess.Start();
            }
        }

        public static void SetWindowEvent(string file, WindowsStyle windowStyle = WindowsStyle.Normal)
        {
            using (Process myProcess = new Process())
            {
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = file;
                myProcess.StartInfo.CreateNoWindow = false;

                myProcess.StartInfo.WindowStyle = Select(windowStyle);
                myProcess.Start();
            }

            /// Click on screen center.
            SetCursorPos(Screen.width / 2, Screen.height / 2);
            //System.Threading.Thread.Sleep(100);
            Mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
        [DllImport("user32")]
        public static extern int ShowWindow(int hwnd, int nCmdShow);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        const uint WM_CLOSE = 0x0010;

        ///// <summary>
        ///// Close Application CloseWindow(Kinect). No need filename extension.
        ///// </summary>
        ///// <param name="windowsName"></param>
        //public static void CloseWindow(string windowsName)
        //{
        //    IntPtr windowPtr = FindWindowByCaption(IntPtr.Zero, windowsName);
        //    if (windowPtr == IntPtr.Zero)
        //    {
        //        UnityEngine.Debug.LogError("'" + windowsName + "' not found!");
        //        return;
        //    }

        //    SendMessage(windowPtr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        //}

        /// <summary>
        /// Close Application CloseWindow(Kinect), no need filename extension.
        /// </summary>
        /// <param name="processName"></param>
        public static void CloseWindow(string processName)
        {
            Process[] p1 = Process.GetProcesses();
            foreach (Process pro in p1)
            {
                if (pro.ProcessName.ToUpper().Contains(processName) || pro.ProcessName.Contains(processName))
                    pro.CloseMainWindow();
            }
        }

        /// <summary>
        /// Open file explorer.
        /// </summary>
        /// <param name="path"></param>
        public static void OpenExplorer(string path = "c:/")
        {
            Process.Start(@path);
        }
        #endregion

        #region Private function
        private static ProcessWindowStyle Select(WindowsStyle style)
        {
            switch (style)
            {
                case WindowsStyle.Hidden:
                    return ProcessWindowStyle.Hidden;
                case WindowsStyle.Maximized:
                    return ProcessWindowStyle.Maximized;
                case WindowsStyle.Minimized:
                    return ProcessWindowStyle.Minimized;
                default:
                    return ProcessWindowStyle.Normal;
            }
        }

        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);

        private const int MOUSEEVENTF_MOVE = 0x0001; /* mouse move */
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002; /* left button down */
        private const int MOUSEEVENTF_LEFTUP = 0x0004; /* left button up */
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008; /* right button down */

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void Mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        #endregion
    }
}