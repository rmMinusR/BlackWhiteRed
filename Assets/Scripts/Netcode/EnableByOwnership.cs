using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class EnableByOwnership : NetworkBehaviour
{
    [Space]
    [SerializeField] private List<GameObject>  localGameobjects = new List<GameObject>();
    [SerializeField] private List<Behaviour >  localBehaviours  = new List<Behaviour >();

    [Space]
    [SerializeField] private List<GameObject> remoteGameobjects = new List<GameObject>();
    [SerializeField] private List<Behaviour > remoteBehaviours  = new List<Behaviour >();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UpdateEnables();
    }

    public void UpdateEnables()
    {
        Dictionary<GameObject, bool> oStates = new Dictionary<GameObject, bool>();
        Dictionary< Behaviour, bool> bStates = new Dictionary< Behaviour, bool>();

        //Figure out what should be enabled
        foreach (GameObject o in  localGameobjects) oStates[o] = (oStates.TryGetValue(o, out bool en)?en:false) ||  IsOwner;
        foreach (Behaviour  b in  localBehaviours ) bStates[b] = (bStates.TryGetValue(b, out bool en)?en:false) ||  IsOwner;
        foreach (GameObject o in remoteGameobjects) oStates[o] = (oStates.TryGetValue(o, out bool en)?en:false) || !IsOwner;
        foreach (Behaviour  b in remoteBehaviours ) bStates[b] = (bStates.TryGetValue(b, out bool en)?en:false) || !IsOwner;

        //Apply it
        foreach (KeyValuePair<GameObject, bool> oState in oStates) oState.Key.SetActive(oState.Value);
        foreach (KeyValuePair<Behaviour , bool> bState in bStates) bState.Key.enabled = bState.Value;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(localBehaviours.RemoveAll(b => b is NetworkBehaviour) > 0)
        {
            UnityEditor.EditorWindow.focusedWindow.ShowNotification(new GUIContent("Toggling NetworkBehaviours is not allowed!"));
        }
    }
#endif

}
