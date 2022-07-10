using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class LogToFile : MonoBehaviour
{
    [SerializeField] [Tooltip("Flushes per second")] [Min(1)] private float flushFrequency = 5;
    [SerializeField] private string logFilePathFormat = "BWR_$date_$time.log";
    private StringBuilder buffer;
    private StreamWriter logFile;

#if UNITY_EDITOR
    [SerializeField] private bool disableInEditor = true;
#endif

    private void Start()
    {
#if UNITY_EDITOR
        if (disableInEditor && Application.isEditor) return;
#endif

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

        buffer = new StringBuilder(256);
        flusherThread = new Thread(ThreadedFlushWorker);
        flusherThread.Name = "Log I/O Thread";
        flusherThread.Start();

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
        lock (buffer)
        {
            buffer.AppendLine(DateTime.Now.ToString("HH:mm:ss")+"\t"+type+"\t"+logString);
            if (type == LogType.Exception) foreach (string traceLine in stackTrace.Split("\n")) buffer.AppendLine("\t\t\t"+traceLine);
        }
    }

    private Thread flusherThread;
    private bool flusherRunning = false;
    private void ThreadedFlushWorker()
    {
        flusherRunning = true;
        while (this != null && logFile != null && flusherRunning)
        {
            //Transfer from buffer to logfile
            string bufferContents;
            lock(buffer)
            {
                bufferContents = buffer.ToString();
                buffer.Clear();
            }
            logFile.Write(bufferContents);

            //Flush logfile to make sure copy on disk is up to date
            Task flushOp = logFile.FlushAsync();
            Thread.Sleep((int) (1000f / flushFrequency));
            flushOp.Wait(); //Ensure flush is finished before starting next cycle
        }
        flusherRunning = false;
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
        //Unhook
        Application.logMessageReceived -= _OnLog;

        Application.wantsToQuit -= Application_wantsToQuit;

        //Halt thread
        if (flusherThread != null)
        {
            flusherRunning = false; //Send stop signal
            //if (flusherThread.IsAlive) flusherThread.Abort();
            flusherThread.Join(500);
            flusherThread = null;
        }

        //Close file
        if (logFile != null)
        {
            logFile.WriteLine(DateTime.Now.ToString("HH:mm:ss")+"\tShutdown");

            logFile.Flush();
            logFile.Close();
            logFile.Dispose();
            logFile = null;
        }
    }
}
