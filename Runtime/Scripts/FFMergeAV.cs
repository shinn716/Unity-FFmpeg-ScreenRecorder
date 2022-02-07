using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading.Tasks;

public class FFMergeAV : MonoBehaviour
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

    [Tooltip("StreamingAssetsPath + [FFmpegPath]")]
    public string FFmpegPath = "/ffmpeg/ffmpeg.exe";

    public string AudioOutput = "X:/test.mp3";
    public string VideoOutput = "X:/test.mp4";

    [Tooltip("If 'CustomFileName' is empty, it'll export by default name.")]
    public string CustomFileName = string.Empty;
    public bool showlog = true;

    private string ffpath = string.Empty;
    private int _pid;
    private bool _isProcess = false;


    private void Start()
    {
        ffpath = Application.streamingAssetsPath + FFmpegPath;

        // Clear ffmpeg Process in memory.
        StartCoroutine(IEExitCmd(() => { print("FFmpeg Init"); }, 0));
        Process[] goDie = Process.GetProcessesByName("ffmpeg");
        foreach (Process p in goDie) p.Kill();
    }

    private void OnDestroy()
    {
        if (_isProcess)
        {
            try
            {
                UnityEngine.Debug.LogError("FFMergeAV::OnDestroy - 錄製進程非正常結束，輸出文件可能無法播放。");
                Process.GetProcessById(_pid).Kill();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("FFMergeAV::OnDestroy - " + e.Message);
            }
        }
    }


    [ContextMenu("GetDevicesList")]
    public void GetDevicesList()
    {
        print("[GetDevicesList]");
        if (_isProcess)
        {
            UnityEngine.Debug.LogError("FFMergeAV::StartProcessing - 當前已有錄製進程。");
            return;
        }

        var _ffargs = "-list_devices true -f dshow -i dummy";
        StartCoroutine(GetDevicesProcess(_ffargs));
    }

    [ContextMenu("StartMerge")]
    public void StartMerge()
    {
        if (_isProcess)
        {
            UnityEngine.Debug.LogError("FFMergeAV::StartRecording - 當前已有錄製進程。");
            return;
        }

        print("[Start FF]");
        var _ffargs = SettingRecordingArgs();
        UnityEngine.Debug.Log("FFMergeAV::StartRecording - 執行命令：" + ffpath + " " + _ffargs);
        StartCoroutine(IEEnterCmd(_ffargs));

        StartCoroutine(IEExitCmd(
            () =>
            {
                print("[Stop FF]");
            }, 3)
            );
    }

    private string SettingRecordingArgs()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "output");
        string fileName = CustomFileName.Equals(String.Empty) ?
                          Path.Combine(path, Application.productName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp4") :
                          Path.Combine(path, CustomFileName + ".mp4");
        var _ffargs = "-i " + VideoOutput + " -i " + AudioOutput + " -c:v copy -c:a aac " + fileName;
        return _ffargs;
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

    private IEnumerator GetDevicesProcess(string args)
    {
        yield return IEEnterCmd(args);
        StartCoroutine(IEExitCmd(
            () => { print("[Stop FF]"); }, 1));
    }

    private IEnumerator IEEnterCmd(string args)
    {
        yield return null;

        Process ffp = new Process();
        ffp.StartInfo.FileName = ffpath;                   // 進程可執行文件位置
        ffp.StartInfo.Arguments = args;                  // 傳給可執行文件的命令行參數
        ffp.StartInfo.CreateNoWindow = true;             // 是否顯示控制檯窗口
        ffp.StartInfo.UseShellExecute = false;             // 是否使用操作系統Shell程序啓動進程

        //ffp.StartInfo.CreateNoWindow = !debugOpenFF;             // 是否顯示控制檯窗口
        //ffp.StartInfo.UseShellExecute = debugOpenFF;             // 是否使用操作系統Shell程序啓動進程
        ffp.StartInfo.Verb = "runas";

        if (showlog)
        {
            ffp.StartInfo.RedirectStandardError = true;
            ffp.ErrorDataReceived += new DataReceivedEventHandler(Output);
        }

        // 開始進程
        _isProcess = ffp.Start();
        _pid = ffp.Id;

        if (showlog)
            ffp.BeginErrorReadLine();
    }

    private IEnumerator IEExitCmd(Action _OnStopRecording, float stopTime = 3)
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
        yield return new WaitForSeconds(stopTime);

        // 卸載控制檯事件的處理句柄，不然之後的ffmpeg調用無法正常停止
        SetConsoleCtrlHandler(IntPtr.Zero, false);
        // 剝離已附加的控制檯
        FreeConsole();

        _isProcess = false;

        if (_OnStopRecording != null)
            _OnStopRecording();
    }
}
