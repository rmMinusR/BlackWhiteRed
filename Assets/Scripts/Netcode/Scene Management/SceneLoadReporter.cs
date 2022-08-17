using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this in scenes to report back to SceneGroupLoader that loading is successful
/// </summary>
public sealed class SceneLoadReporter : MonoBehaviour
{
    [SerializeField] private bool isSingleton = true;

    private void Awake()
    {
        SceneGroupLoader.Instance.OnSceneLoaded(gameObject.scene, isSingleton);
    }

    private void OnDestroy()
    {
        SceneGroupLoader.Instance.OnSceneUnloaded(gameObject.scene);
    }
}
