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
        Debug.Assert(Instance != null);
        Instance = this;
    }

    private void OnDestroy()
    {
        Debug.Assert(Instance == this);
        Instance = null;
    }

    #endregion

    #region Sentinel reporting callbacks

    [SerializeField] private List<Scene> loaded = new List<Scene>();

    internal void OnSceneLoaded(Scene scene, bool isSingleton)
    {
        bool multipleInstances = loaded.Contains(scene);
        loaded.Add(scene);
        if (isSingleton && multipleInstances) throw new InvalidOperationException("Singleton Scene "+scene.name+" loaded multiple times! This is not allowed!");
    }

    internal void OnSceneUnloaded(Scene scene)
    {
        bool success = loaded.Remove(scene);
        if (!success) throw new InvalidOperationException("Scene "+scene.name+" unloaded more times than it was loaded!");
    }

    #endregion

    #region Public interface

    private bool isBusy = false;

    public (Task task, LoadOp progress) LoadSceneGroupAsync(params string[] names)
    {
        LoadOp reporting = new LoadOp(names.Length);
        Task task = AsyncSceneGroupLoadWorker(names.Select(n => UnitySceneManager.GetSceneByName(n)).ToList(), reporting);
        return (task, reporting);
    }

    private async Task AsyncSceneGroupLoadWorker(List<Scene> scenes, LoadOp reporting)
    {
        if (!isBusy) throw new InvalidOperationException("A scene group is already loading!");
        isBusy = true;

        //Mark current root gameobjects for death
        List<GameObject> toKill = new List<GameObject>();
        Scene ddol = GetDDOLScene();
        foreach (Transform t in FindObjectsOfType<Transform>())
        {
            if (t.parent == null && t.gameObject.scene != ddol) toKill.Add(t.gameObject);
        }

        //Begin loading
        for(int i = 0; i < scenes.Count; ++i)
        {
            //Set up load operation
            reporting.CurrentlyLoading = scenes[i];
            reporting.currentOp = UnitySceneManager.LoadSceneAsync(reporting.CurrentlyLoading.buildIndex, LoadSceneMode.Additive);

            //Wait for finish
            while (!reporting.currentOp.isDone) await Task.Delay(10);

            //Report
            reporting.currentlyLoaded++;
        }

        //Kill marked objects
        foreach (GameObject k in toKill) Destroy(k);

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
        public Scene CurrentlyLoading { get; internal set; }
    }

    #endregion

    /// <summary>
    /// Objects marked with DontDestroyOnLoad go in a special scene at runtime
    /// </summary>
    private static Scene GetDDOLScene()
    {
        GameObject temp = new GameObject();
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