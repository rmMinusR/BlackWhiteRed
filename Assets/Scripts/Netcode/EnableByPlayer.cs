using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class EnableByPlayer : NetworkBehaviour
{
    [Space]
    [SerializeField] private List<GameObject>  selfGameobjects = new List<GameObject>();
    [SerializeField] private List<Behaviour >  selfBehaviours  = new List<Behaviour >();

    [Space]
    [SerializeField] private List<GameObject> otherGameobjects = new List<GameObject>();
    [SerializeField] private List<Behaviour > otherBehaviours  = new List<Behaviour >();

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
        foreach (GameObject o in  selfGameobjects) oStates[o] = (oStates.TryGetValue(o, out bool en)?en:false) ||  IsLocalPlayer;
        foreach (Behaviour  b in  selfBehaviours ) bStates[b] = (bStates.TryGetValue(b, out bool en)?en:false) ||  IsLocalPlayer;
        foreach (GameObject o in otherGameobjects) oStates[o] = (oStates.TryGetValue(o, out bool en)?en:false) || !IsLocalPlayer;
        foreach (Behaviour  b in otherBehaviours ) bStates[b] = (bStates.TryGetValue(b, out bool en)?en:false) || !IsLocalPlayer;

        //Apply it
        foreach (KeyValuePair<GameObject, bool> oState in oStates) oState.Key.SetActive(oState.Value);
        foreach (KeyValuePair<Behaviour , bool> bState in bStates) bState.Key.enabled = bState.Value;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(selfBehaviours.RemoveAll(b => b is NetworkBehaviour) > 0)
        {
            UnityEditor.EditorWindow.focusedWindow.ShowNotification(new GUIContent("Toggling NetworkBehaviours is not allowed!"));
        }
    }
#endif

}
