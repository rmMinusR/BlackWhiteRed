using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using FMOD;

public class SpatializedSoundSystem : MonoBehaviour
{
    public static SpatializedSoundSystem Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Init();
            return;
        }

        UnityEngine.Debug.LogError("SpatializedSoundSystem Instance already exists, deleting " + this.name);
        Destroy(this);
    }

    private void Init()
    {
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    /// <summary>
    /// Play 3D sound with no concern for time it will play for (release it so it deletes one the event is done)
    /// </summary>
    /// <param name="eventName"></param>
    public void PlayReleasedSpatializedSound(FMODUnity.EventReference eventName, Vector3 pos)
    {
        UnityEngine.Debug.Log(eventName.ToString() + ": played at pos " + pos);
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventName);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(pos));
        instance.start();
        instance.release();
    }

    /// <summary>
    /// Play and return 3D sound to be stopped or have its parameters edited
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public FMOD.Studio.EventInstance PlayTrackedSpatializedSound(FMODUnity.EventReference eventName, Vector3 pos)
    {
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventName);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(pos));
        instance.start();
        return instance;
    }

    public void PlayReleasedSpatializedSoundAttached(FMODUnity.EventReference eventName, Transform transform)
    {
        UnityEngine.Debug.Log(eventName.ToString() + ": played at pos " + transform.position);
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventName);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(instance, transform);
        //instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(pos));
        instance.start();
        instance.release();
    }
}
