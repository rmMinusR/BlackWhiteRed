using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviour
{
    //const string SceneNameLevelDesign = "Level3-Area0-LevelDesign";
    //const string SceneNameEnvironmentArt = "Level3-Area0-EnvironmentArt";

    //void Start()
    //{
    //    NetworkManager.Singleton.SceneManager.LoadScene(SceneNameLevelDesign, LoadSceneMode.Additive);
    //}

    //private void OnEnable()
    //{
    //    NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleNewSceneLoaded;
    //}

    //private void OnDisable()
    //{
    //    NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleNewSceneLoaded;
    //}

    //private void HandleNewSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    //{
    //    if(NetworkManager.Singleton.IsHost)
    //    {
    //        if(sceneName == SceneNameLevelDesign)
    //        {
    //            NetworkManager.Singleton.SceneManager.LoadScene(SceneNameEnvironmentArt, LoadSceneMode.Additive);
    //        }
    //        else if(sceneName == SceneNameEnvironmentArt)
    //        {
    //            Debug.Log("MATCH CAN START");
    //        }
    //    }
    //}

}
