using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class SceneGroupLoader : MonoBehaviour
{
    #region Singleton

    public static SceneGroupLoader Instance { get; private set; }

    private void Awake()
    {
        Debug.Assert(Instance == null);
        Instance = this;

        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
        UnitySceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnitySceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        UnitySceneManager.sceneUnloaded -= OnSceneUnloaded;

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
        LoadOp reporting = new LoadOp(names.Length);
        StartCoroutine(AsyncSceneGroupLoadWorker(names.ToList(), reporting));
        return reporting;
    }

    private IEnumerator AsyncSceneGroupLoadWorker(List<string> scenes, LoadOp reporting)
    {
        if (isBusy) throw new InvalidOperationException("A scene group is already loading!");
        isBusy = true;

        yield return null;
        Debug.Log($"Starting load for {scenes.Count} scenes");

        //Mark current root gameobjects for death
        List<GameObject> toKill = new List<GameObject>();
        Scene ddol = GetDDOLScene();
#if true
        foreach (Scene s in loaded) toKill.AddRange(s.GetRootGameObjects());
#else
        foreach (Transform t in FindObjectsOfType<Transform>())
        {
            if (t.parent == null && t.gameObject.scene != ddol) toKill.Add(t.gameObject);
        }
#endif
        Debug.Log($"Found {toKill.Count} root objects to destroy");

        //Begin loading
        for(int i = 0; i < scenes.Count; ++i)
        {
            //Set up load operation
            reporting.CurrentlyLoading = scenes[i];
            Debug.Log($"Loading {reporting.CurrentlyLoading}...");
            reporting.currentOp = UnitySceneManager.LoadSceneAsync(reporting.CurrentlyLoading, LoadSceneMode.Additive);

            //Wait for finish
            while (!reporting.currentOp.isDone) yield return new WaitForSecondsRealtime(0.1f);
            Debug.Log($"Done loading {reporting.CurrentlyLoading}");

            //Report
            reporting.currentlyLoaded++;
        }

        Debug.Log("Finished loading all scene objects");

        //Kill marked objects
        foreach (GameObject k in toKill) if (k.scene != ddol) Destroy(k);
        Debug.Log("Destroyed all objects from other scenes");

        isBusy = false;

        reporting.__onComplete();
    }

    public sealed class LoadOp
    {
        //We don't have to lock anything since 'async' is concurrent and not threaded

        internal int currentlyLoaded;
        internal int totalToLoad;
        internal AsyncOperation currentOp;

        internal LoadOp(int totalToLoad)
        {
            currentlyLoaded = 0;
            this.totalToLoad = totalToLoad;
            currentOp = null;
        }

        internal void __onComplete() => onComplete?.Invoke();
        public event Action onComplete;

        public float Progress => (currentlyLoaded + currentOp.progress) / totalToLoad;
        public string CurrentlyLoading { get; internal set; }
    }

#endregion

    /// <summary>
    /// Objects marked with DontDestroyOnLoad go in a special scene at runtime
    /// </summary>
    private static Scene GetDDOLScene()
    {
        GameObject temp = new GameObject();
        DontDestroyOnLoad(temp);
        try
        {
            return temp.scene;
        }
        finally
        {
            DestroyImmediate(temp);
        }
    }
}