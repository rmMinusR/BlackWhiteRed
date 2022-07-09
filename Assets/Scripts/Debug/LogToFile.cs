using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public sealed class LogToFile : MonoBehaviour
{
    [SerializeField] private bool disableInEditor = true;

    [SerializeField] [Tooltip("Flushes per second")] [Min(1)] private float flushFrequency = 5;
    [SerializeField] private string logFilePathFormat = "BWR_$date_$time.log";
    private StreamWriter logFile;

    private void Start()
    {
        if (disableInEditor && Application.isEditor) return;

        //Apply path format
        string logFilePath = logFilePathFormat
                                .Replace("$date", DateTime.Now.ToString("yy.MM.dd"))
                                .Replace("$time", DateTime.Now.ToString("zz.HH.mm.ss"));

        //Make sure the parent folder exists
        if (logFilePath.Contains("/"))
        {
            DirectoryInfo dir = new DirectoryInfo(logFilePath.Substring(0, logFilePath.LastIndexOf("/")));
            if (!dir.Exists) dir.Create();
        }

        //Open file
        logFile = File.AppendText(logFilePath);
        
        //Write build info
        logFile.WriteLine("Black White Red v"+Application.version+" for "+Application.platform.ToString().Replace("Player", ""));
        logFile.WriteLine("Build #"+BuildInfo.BUILD_NUMBER+" ("+BuildInfo.BUILD_TIME.ToString("MMM dd HH:mm:ss")+") with framework v"+Application.unityVersion);
        logFile.WriteLine(Application.genuineCheckAvailable
            ? (Application.genuine ? "Contents not modified" : "Contents modified")
            : "Could not verify contents");
        logFile.Flush();

        StartCoroutine(FlushWorker());

        //Hook into log events
        Debug.unityLogger.logEnabled = true;
        Application.logMessageReceived -= _OnLog;
        Application.logMessageReceived += _OnLog;

        //(Build only) hook into quit event
        Application.wantsToQuit -= Application_wantsToQuit;
        Application.wantsToQuit += Application_wantsToQuit;
    }

    private void _OnLog(string logString, string stackTrace, LogType type)
    {
        logFile.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss")+"\t"+type+"\t"+logString);
        if(type == LogType.Exception) logFile.WriteLineAsync(stackTrace);
    }

    private IEnumerator FlushWorker()
    {
        while (true)
        {
            Task f = logFile.FlushAsync();
            yield return new WaitForSecondsRealtime(1f/flushFrequency);
            yield return new WaitForTask(f); //Ensure flush is finished before starting next one
        }
    }

    private void OnDestroy() => Cleanup();
    private void OnApplicationQuit() => Cleanup();
    ~LogToFile() => Cleanup();

    //Build only
    private bool Application_wantsToQuit()
    {
        Cleanup();
        return true;
    }

    private void Cleanup()
    {
        StopAllCoroutines();

        //Unhook
        Application.logMessageReceived -= _OnLog;

        Application.wantsToQuit -= Application_wantsToQuit;

        //Close file
        if (logFile != null)
        {
            _OnLog("Shutdown", "", LogType.Log);

            logFile.Flush();
            logFile.Close();
            logFile.Dispose();
            logFile = null;
        }
    }
}
