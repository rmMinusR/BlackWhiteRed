using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class LogToUIText : MonoBehaviour
{
    private TMP_Text target;

    private void Start()
    {
        target = GetComponent<TMP_Text>();
        target.text = "";

        //Hook into log event
        Application.logMessageReceived -= _OnLog;
        Application.logMessageReceived += _OnLog;
    }

    private void _OnLog(string logString, string stackTrace, LogType type)
    {
        string line = type+"> " + logString;
        target.text = line + "\n" + target.text;
    }

    private void OnDestroy()
    {
        //Unhook
        Application.logMessageReceived -= _OnLog;
    }
}
