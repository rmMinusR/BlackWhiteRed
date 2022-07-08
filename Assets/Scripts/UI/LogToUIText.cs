using System;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class LogToUIText : MonoBehaviour
{
    [SerializeField] [Min(0)] private int maxChars = 100;
    private StringBuilder buffer;
    private TMP_Text target;

    private void Start()
    {
        target = GetComponent<TMP_Text>();
        target.text = "";

        buffer = new StringBuilder(256);

        //Hook into log event
        Application.logMessageReceived -= _OnLog;
        Application.logMessageReceived += _OnLog;
    }

    private void _OnLog(string logString, string stackTrace, LogType type)
    {
        string line = type+"> " + logString;
        buffer.AppendLine(line);
    }

    private void Update()
    {
        //Try to append
        target.text += buffer.ToString();
        buffer.Clear();

        //Try to trim
        int toTruncate = target.text.Length-maxChars; //Minimum
        if (toTruncate > 0)
        {
            //Search for next newline
            toTruncate = target.text.IndexOf('\n', toTruncate) + 1;

            //Truncate
            target.text = target.text.Substring(toTruncate);
        }
    }

    private void OnDestroy()
    {
        //Unhook
        Application.logMessageReceived -= _OnLog;
    }
}
