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

public class FFScreenRecorder : MonoBehaviour
{
    #region 
    [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Ansi)]
    public static extern int SetWindowText(int hwnd, string lpString);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    #endregion

    [Tooltip("StreamingAssetsPath + [FFmpegPath]")]
    public string ffmpegPath = "/ffmpeg/ffmpeg.exe";

    [Tooltip("H.264 NVIDIA (MP4)\n" +
                "H.264 Lossless 420 (MP4)\n" +
                "H.264 Lossless 444 (MP4)\n" +
                "HEVC Default (MP4)\n" +
                "HEVC NVIDIA (MP4)\n" +
                "ProRes 422 (QuickTime)\n" +
                "ProRes 4444 (QuickTime)\n" +
                "VP8 (WebM)\n" +
                "VP9 (WebM)\n" +
                "HAP (QuickTime)\n" +
                "HAP Alpha (QuickTime)\n" +
                "HAP Q (QuickTime)"
        )]
    public FFmpegOut.FFmpegPreset ffmpegPreset = FFmpegOut.FFmpegPreset.H264Default;
    public int framerate = 30;
    public Vector2Int captureSize = new Vector2Int(1920, 1080);
    public Vector2Int offsetPos = new Vector2Int(0, 0);

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
                          Path.Combine(path, Application.productName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + FFmpegOut.FFmpegPresetExtensions.GetSuffix(ffmpegPreset)) :
                          Path.Combine(path, CustomFileName + FFmpegOut.FFmpegPresetExtensions.GetSuffix(ffmpegPreset));

        var offset = "-offset_x " + offsetPos.x + " -offset_y " + offsetPos.y;
        var resolution = "-video_size " + captureSize.x + "x" + captureSize.y;

        if (captureSize.x == 0 || captureSize.y == 0)
            resolution = "";

        var option = FFmpegOut.FFmpegPresetExtensions.GetOptions(ffmpegPreset);
        GetOutputName = fileName;

        var args = "-rtbufsize 1500M -f dshow -i audio=\"" + AudioInput + "\" -f -y -rtbufsize 100M -f gdigrab " + offset + " " + resolution + " -framerate " + framerate + " -probesize 10M -draw_mouse 0 -i desktop -c:v libx264 -r 30 -preset ultrafast -tune zerolatency -crf 25 " + option + " \"" + fileName + "\"";
        
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
