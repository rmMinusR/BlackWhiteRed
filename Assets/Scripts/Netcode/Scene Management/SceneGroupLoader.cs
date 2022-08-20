using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneGroupLoader : MonoBehaviour
{
    #region Singleton

    public static SceneGroupLoader Instance { get; private set; }

    private void Awake()
    {
        Debug.Assert(Instance == null);
        Instance = this;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        Debug.Assert(Instance == this);
        Instance = null;
    }

    #endregion

    #region Scene loading callbacks

    [SerializeField] private List<Scene> loaded = new List<Scene>();

    internal void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) loaded.Clear();
        loaded.Add(scene);
    }

    internal void OnSceneUnloaded(Scene scene)
    {
        bool success = loaded.Remove(scene);
        if (!success) throw new InvalidOperationException("Scene "+scene.name+" unloaded more times than it was loaded!");
    }

    #endregion

    #region Public interface

    private bool isBusy = false;

    public LoadOp LoadSceneGroupAsync(params string[] names)
    {
        Debug.Log($"Received load command for {names.Length} scenes");
        LoadOp reporting = new LoadOp(loaded.Count, names.Length);
        StartCoroutine(AsyncSceneGroupLoadWorker(names.ToList(), reporting));
        return reporting;
    }

    private IEnumerator AsyncSceneGroupLoadWorker(List<string> toLoad, LoadOp reporting)
    {
        if (isBusy) throw new InvalidOperationException("A scene group is already loading!");
        isBusy = true;

        yield return null;

        reporting.unloadTarget = loaded.Count;
        reporting.  loadTarget = toLoad.Count;

        //Unload what's currently loaded
        Debug.Log($"Starting unload {loaded.Count} scenes");
        List<Scene> toUnload = new List<Scene>(loaded);
        while (toUnload.Count > 0)
        {
            //Set up unload operation
            reporting.current.target = toUnload[0].name;
            Debug.Log($"Unloading {reporting.current.target}...");
            AsyncOperation op = SceneManager.UnloadSceneAsync(toUnload[0]);

            //Wait for finish
            while (!op.isDone) yield return null;
            toUnload.RemoveAt(0);

            //Report
            reporting.unloadedNow++;
        }
        Debug.Log("Finished unloading all scenes");

        //Begin loading new scenes
        Debug.Log($"Starting load for {toLoad.Count} scenes");
        for(int i = 0; i < toLoad.Count; ++i)
        {
            //Set up load operation
            reporting.current.target = toLoad[i];
            Debug.Log($"Loading {reporting.current.target}...");
            reporting.current.op = SceneManager.LoadSceneAsync(reporting.current.target, LoadSceneMode.Additive);

            //Wait for finish
            while (!reporting.current.op.isDone) yield return new WaitForSecondsRealtime(0.1f);

            //Report
            reporting.loadedNow++;
        }
        Debug.Log("Finished loading all scene objects");

        isBusy = false;

        reporting.__onComplete();
    }

    public sealed class LoadOp
    {
        internal enum OpType
        {
            None = 0,
            Unload,
            Load
        }

        //We don't have to lock anything since 'async' is concurrent and not threaded
        internal (AsyncOperation op, OpType type, string target) current;

        internal int unloadedNow;
        internal int unloadTarget;
        public float UnloadProgress => (unloadedNow + current.type == OpType.Unload ? current.op?.progress ?? 0 : 0) / unloadTarget;

        internal int loadedNow;
        internal int loadTarget;
        public float LoadProgress => (loadedNow + current.type == OpType.Load ? current.op?.progress ?? 0 : 0) / loadTarget;

        internal LoadOp(int unloadTarget, int loadTarget)
        {
            unloadedNow = loadedNow = 0;
            this.unloadTarget = unloadTarget;
            this.loadTarget = loadTarget;
        }

        internal void __onComplete()
        {
            onComplete?.Invoke();
            isDone = true;
        }

        public event Action onComplete;
        public bool isDone { get; private set; }

        public float TotalProgress => (unloadedNow + loadedNow + current.op?.progress??0) / (unloadTarget + loadTarget);
    }

#endregion
}