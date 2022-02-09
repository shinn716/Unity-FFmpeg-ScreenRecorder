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
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class FFFrameRecorder : MonoBehaviour
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


    public Camera TargetCam;
    public int framerate = 30;
    public Vector2Int captureSize = new Vector2Int(1280, 720);

    public bool showfflog = true;
    public string OutputFolder = "Output";

    [Tooltip("If 'CustomFileName' is empty, it'll export by default name.")]
    public string CustomFileName = string.Empty;

    public string GetOutputName { get; set; }

    private string imagePath;
    private string ffpath = string.Empty;

    private int index = 0;
    private Texture2D frame;
    private RenderTexture rt;
    private Rect rect;
    private byte[] bytes;

    void Start()
    {
        ffpath = @Application.streamingAssetsPath + ffmpegPath;
        imagePath = @Application.streamingAssetsPath + "/FFTemp~/";

        if (!Directory.Exists(imagePath.TrimEnd('/')))
            Directory.CreateDirectory(imagePath.TrimEnd('/'));

        if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, OutputFolder).TrimEnd('/')))
            Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, OutputFolder).TrimEnd('/'));

        IntPtr handle = GetForegroundWindow();
        SetWindowText(handle.ToInt32(), Application.productName);

        rect = new Rect(0, 0, captureSize.x, captureSize.y);
        rt = new RenderTexture(captureSize.x, captureSize.y, 0);
        frame = new Texture2D(captureSize.x, captureSize.y, TextureFormat.RGBA32, false);
    }

    [ContextMenu("StartRecording")]
    public void StartRecording()
    {
        if (TargetCam == null)
        {
            UnityEngine.Debug.Log("FrameRecorder has no camera.");
            return;
        }

        print("[Log] Start Recording...");
        AsyncCapture(cts.Token);
    }

    CancellationTokenSource cts = new CancellationTokenSource();

    [ContextMenu("StopRecording")]
    public void StopRecording()
    {
        print("[Log] Stop Record");
        cts.Cancel();
        AsyncCreateProcess();
    }


    async private void AsyncCapture(CancellationToken ct)
    {
        if (TargetCam == null)
        {
            UnityEngine.Debug.Log("FrameRecorder has no camera.");
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            CaptureCamera(TargetCam, index);
            index++;
            await Task.Delay(1000 / framerate);
        }
    }

    async private void AsyncCreateProcess()
    {
        Process p = new Process();
        var path = Path.Combine(Application.streamingAssetsPath, OutputFolder);

        string fileName = CustomFileName.Equals(String.Empty) ?
                  Path.Combine(path, Application.productName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + FFmpegOut.FFmpegPresetExtensions.GetSuffix(ffmpegPreset)) :
                  Path.Combine(path, CustomFileName + FFmpegOut.FFmpegPresetExtensions.GetSuffix(ffmpegPreset));

        GetOutputName = fileName;

        var task01 = Task.Run(() =>
        {
            p.StartInfo.FileName = ffpath;
            string args = "-f image2 -i " + imagePath + "%d.jpg -vcodec libx264 -r 25 " + fileName;
            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            if (showfflog)
                p.ErrorDataReceived += new DataReceivedEventHandler(Output);
        });
        task01.Wait();

        var task02 = Task.Run(() =>
        {
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            p.Dispose();

            DirectoryInfo dir = new DirectoryInfo(imagePath);
            dir.Delete(true);
            //Directory.CreateDirectory(imagePath.TrimEnd('/'));
            print("[Log] Output: " + GetOutputName);
        });
        //task02.Wait();

        await Task.Yield();
    }

    private void Output(object sendProcess, DataReceivedEventArgs output)
    {
        if (!string.IsNullOrEmpty(output.Data))
            UnityEngine.Debug.Log(output.Data);
    }

    async private void CaptureCamera(Camera camera, int index)
    {
        camera.targetTexture = rt;
        camera.Render();

        RenderTexture.active = rt;
        frame.ReadPixels(rect, 0, 0);
        frame.Apply();

        camera.targetTexture = null;
        RenderTexture.active = null;
        bytes = frame.EncodeToJPG();

        // to jpg
        var task01 = Task.Run(() =>
        {
            File.WriteAllBytes(imagePath + index + ".jpg", bytes);
        });
        await Task.Yield();
    }
}
