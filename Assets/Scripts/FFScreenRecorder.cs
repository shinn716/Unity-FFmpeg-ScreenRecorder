// Sources: https://www.twblogs.net/a/5ef0c1884b16c91a2849601a

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class FFScreenRecorder : MonoBehaviour
{
    #region 模擬控制檯信號需要使用的DLL
    [DllImport("kernel32.dll")]
    static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);
    [DllImport("kernel32.dll")]
    static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);
    [DllImport("kernel32.dll")]
    static extern bool AttachConsole(int dwProcessId);
    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();
    #endregion

    public Vector2Int CaptureSize = new Vector2Int(1920, 1080);
    public string AudioInput = "Microphone (Realtek(R) Audio)";

    public bool debugOpenFF = false;
    public bool showlog = true;

    private string _ffpath = string.Empty;
    private string _ffargs = string.Empty;

    private int _pid;
    private bool _isRecording = false;


    private void Start()
    {
        _ffpath = Application.streamingAssetsPath + "/ffmpeg/ffmpeg.exe";
    }

    private void OnDestroy()
    {
        if (_isRecording)
        {
            try
            {
                UnityEngine.Debug.LogError("FFRecorder::OnDestroy - 錄製進程非正常結束，輸出文件可能無法播放。");
                Process.GetProcessById(_pid).Kill();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("FFRecorder::OnDestroy - " + e.Message);
            }
        }
    }


    [ContextMenu("GetDevicesList")]
    public void GetDevicesList()
    {
        print("[GetDevicesList]");
        if (_isRecording)
        {
            UnityEngine.Debug.LogError("FFRecorder::StartRecording - 當前已有錄製進程。");
            return;
        }

        Process[] goDie = Process.GetProcessesByName("ffmpeg");
        foreach (Process p in goDie) p.Kill();

        _ffargs = "-list_devices true -f dshow -i dummy";
        StartCoroutine(IERecording());
    }

    [ContextMenu("StartRecording")]
    public void StartRecording()
    {
        print("[Start FF]");
        if (_isRecording)
        {
            UnityEngine.Debug.LogError("FFRecorder::StartRecording - 當前已有錄製進程。");
            return;
        }

        // 殺死已有的ffmpeg進程，不要加.exe後綴
        Process[] goDie = Process.GetProcessesByName("ffmpeg");
        foreach (Process p in goDie) p.Kill();

        // 解析設置，如果設置正確，則開始錄製
        bool validSettings = ParseSettings();
        if (validSettings)
        {
            UnityEngine.Debug.Log("FFRecorder::StartRecording - 執行命令：" + _ffpath + " " + _ffargs);
            StartCoroutine(IERecording());
        }
        else
        {
            UnityEngine.Debug.LogError("FFRecorder::StartRecording - 設置不當，錄製取消，請檢查控制檯輸出。");
        }
    }

    [ContextMenu("StopRecording")]
    public void StopRecording()
    {
        StopRecording(() => {
            print("[Stop FF]");
        });
    }


    private void StopRecording(Action _OnStopRecording)
    {
        if (!_isRecording)
        {
            UnityEngine.Debug.LogError("FFRecorder::StopRecording - 當前沒有錄製進程，已取消操作。");
            return;
        }

        StartCoroutine(IEExitCmd(_OnStopRecording));
    }

    private bool ParseSettings()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "output");
        var fileName = Path.Combine(path, Application.productName + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".mp4");
        var resolution = "-video_size " + CaptureSize.x + "x" + CaptureSize.y;
        if (CaptureSize.x == 0 || CaptureSize.y == 0)
            resolution = "";

        _ffargs = "-rtbufsize 1500M -f dshow -i audio=\"" + AudioInput + "\" -f -y -rtbufsize 100M -f gdigrab -t 00:00:30 " + resolution + " -framerate 30 -probesize 10M -draw_mouse 0 -i desktop -c:v libx264 -r 30 -preset ultrafast -tune zerolatency -crf 25 -pix_fmt yuv420p \"" + fileName + "\"";
        return true;
    }

    private void Output(object sendProcess, DataReceivedEventArgs output)
    {
        if (!string.IsNullOrEmpty(output.Data))
        {
            UnityEngine.Debug.Log(output.Data);
            if (output.Data.Contains("I/O error"))
                UnityEngine.Debug.LogError(output.Data);
        }
    }

    private IEnumerator IERecording()
    {
        yield return null;

        Process ffp = new Process();
        ffp.StartInfo.FileName = _ffpath;                   // 進程可執行文件位置
        ffp.StartInfo.Arguments = _ffargs;                  // 傳給可執行文件的命令行參數
        ffp.StartInfo.CreateNoWindow = !debugOpenFF;             // 是否顯示控制檯窗口
        ffp.StartInfo.UseShellExecute = debugOpenFF;             // 是否使用操作系統Shell程序啓動進程
        ffp.StartInfo.Verb = "runas";

        if (showlog)
        {
            ffp.StartInfo.RedirectStandardError = true;
            ffp.ErrorDataReceived += new DataReceivedEventHandler(Output);
        }

        // 開始進程
        _isRecording = ffp.Start();
        _pid = ffp.Id;

        if (showlog)
            ffp.BeginErrorReadLine();
    }

    private IEnumerator IEExitCmd(Action _OnStopRecording)
    {
        // 將當前進程附加到pid進程的控制檯
        AttachConsole(_pid);
        // 將控制檯事件的處理句柄設爲Zero，即當前進程不響應控制檯事件
        // 避免在向控制檯發送【Ctrl C】指令時連帶當前進程一起結束
        SetConsoleCtrlHandler(IntPtr.Zero, true);
        // 向控制檯發送 【Ctrl C】結束指令
        // ffmpeg會收到該指令停止錄製
        GenerateConsoleCtrlEvent(0, 0);

        // ffmpeg不能立即停止，等待一會，否則視頻無法播放
        yield return new WaitForSeconds(3.0f);

        // 卸載控制檯事件的處理句柄，不然之後的ffmpeg調用無法正常停止
        SetConsoleCtrlHandler(IntPtr.Zero, false);
        // 剝離已附加的控制檯
        FreeConsole();

        _isRecording = false;

        if (_OnStopRecording != null)
            _OnStopRecording();
    }
}