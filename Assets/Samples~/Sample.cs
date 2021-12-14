using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    public FFScreenRecorder screenRecorder;
    public Text recordtxt;

    public void Record(bool ison)
    {
        StartCoroutine(Process(ison, recordtxt.transform.parent.GetComponent<Toggle>()));
    }

    public void OnenFolder()
    {
        ScreenRecorder.WindowsEventAPI.OpenExplorer(Path.Combine(Application.streamingAssetsPath, "output"));
    }

    private IEnumerator Process(bool ison, Toggle btnui, float delay = 3)
    {
        btnui.interactable = false;
        if (ison)
        {
            recordtxt.text = "Stop";
            screenRecorder.StartRecording();
        }
        else
        {
            recordtxt.text = "Start Recordinig";
            screenRecorder.StopRecording();
        }
        yield return new WaitForSeconds(delay);
        btnui.interactable = true;
    }

}
