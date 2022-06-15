using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    [SerializeField] [Min(0)] internal int maxConnections;

    public static RelayManager Instance { get; private set; }

    [SerializeField] private RelayConnection _connection; //TODO make inspector read-only
    public RelayConnection Connection => _connection;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogError("Only one RelayManager may exist! Destroying "+this);
            Destroy(gameObject);
        }
    }

    internal static RelayServerEndpoint SelectEndpoint(List<RelayServerEndpoint> endpoints) => endpoints.Find(e => e.ConnectionType == "dtls"); //Use DTLS encryption
}

public abstract class RelayConnection : MonoBehaviour
{
}