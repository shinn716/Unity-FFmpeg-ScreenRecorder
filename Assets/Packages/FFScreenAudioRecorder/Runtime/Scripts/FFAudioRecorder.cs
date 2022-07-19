// 
//  Created by John.Tsai on 2022/2/8
//  Copyright © 2022 John.Tsai. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

[ExecuteInEditMode]
public class FFAudioRecorder : MonoBehaviour
{
    #region 
    [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Ansi)]
    public static extern int SetWindowText(int hwnd, string lpString);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    #endregion

    [Tooltip("StreamingAssetsPath + [FFmpegPath]")]
    public string ffmpegPath = "/ffmpeg/ffmpeg.exe"; 
    public string AudioInput = "Microphone (Realtek(R) Audio)";
    public bool showfflog = true;

    public string OutputFolder = "Output";

    [Tooltip("If 'CustomFileName' is empty, it'll export by default name.")]
    public string CustomFileName = string.Empty;

    public string GetOutputName { get; set; }

    private string ffpath = string.Empty;
    //private int processId = 0;
    private Process process = null;
    private Process process_console = null;

    void Start()
    {
        ffpath = @Application.streamingAssetsPath + ffmpegPath;

        if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, OutputFolder).TrimEnd('/')))
            Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, OutputFolder).TrimEnd('/'));

        IntPtr handle = GetForegroundWindow();
        SetWindowText(handle.ToInt32(), Application.productName);
    }

    private void OnApplicationQuit()
    {
        if (process != null)
        {
            process.StandardInput.WriteLine("q");
            process.WaitForExit();
            process.Close();
        }

        if (process_console != null)
        {
            //process_console.StandardInput.WriteLine("q");
            process_console.WaitForExit();
            process_console.Close();
        }
    }

    [ContextMenu("GetDevicesList")]
    public void GetDevicesList()
    {
        var args = "-list_devices true -f dshow -i dummy";
        Process(ref process_console, args); 
    }

    [ContextMenu("StartRecording")]
    public void StartRecording()
    {
        print("[Log] Start Recording...");
        // Set output path
        var path = Path.Combine(Application.streamingAssetsPath, OutputFolder);
        string fileName = CustomFileName.Equals(String.Empty) ?
                          Path.Combine(path, Application.productName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp3") :
                          Path.Combine(path, CustomFileName + ".mp3");
        var args = "-f dshow -i audio=\"" + AudioInput + "\" -acodec libmp3lame " + " \"" + fileName + "\"";
        GetOutputName = fileName;

        var task = Task.Run(() =>
        {
            Process(ref process, args);
        });
    }

    [ContextMenu("StopRecording")]
    async public void StopRecording()
    {
        if (process != null)
        {
            print("[Log] Stop Record");
            process.StandardInput.WriteLine("q");
            process.WaitForExit();
            process.Close();
            process = null;

            await Task.Delay(1000);
            print("[Log] Output: " + GetOutputName);
        }
    }

    private void Process(ref Process tprocess, string args)
    {
        tprocess = new Process();
        tprocess.StartInfo.FileName = ffpath;

        // Set args into ffmepg
        tprocess.StartInfo.Arguments = args;
        tprocess.StartInfo.UseShellExecute = false;
        tprocess.StartInfo.RedirectStandardError = true;
        tprocess.StartInfo.RedirectStandardInput = true;
        tprocess.StartInfo.RedirectStandardOutput = true;
        tprocess.StartInfo.CreateNoWindow = true;

        if (showfflog)
            tprocess.ErrorDataReceived += new DataReceivedEventHandler(Output);

        tprocess.Start();
        //processId = tprocess.Id;
        //UnityEngine.Debug.Log("processId:" + processId);
        tprocess.BeginErrorReadLine();
    }
    private void Output(object sendProcess, DataReceivedEventArgs output)
    {
        if (!string.IsNullOrEmpty(output.Data))
            UnityEngine.Debug.Log(output.Data);
    }
}
