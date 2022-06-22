using System;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    [SerializeField] [Min(0)] internal int maxConnections;

    public static RelayManager Instance { get; private set; }

    [SerializeField] private BaseRelayConnection _connection; //TODO make inspector read-only
    public BaseRelayConnection Connection => _connection;

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

    public void StartAsHost()
    {
        if (_connection) throw new InvalidOperationException("Already connected!");

        _connection = RelayConnectionHost.New(gameObject);
    }

    public void StartAsClient(string relayJoinCode)
    {
        if (_connection) throw new InvalidOperationException("Already connected!");

        _connection = RelayConnectionClient.New(gameObject, relayJoinCode);
    }

    #region Testing-only code

    private string relayManualJoinCode;
    private void OnGUI()
    {
        if (Connection == null)
        {
            if(GUILayout.Button("               Host               ")) StartAsHost();

            GUILayout.BeginHorizontal();
            relayManualJoinCode = GUILayout.TextField(relayManualJoinCode);
            bool launchClient = GUILayout.Button("Client");
            GUILayout.EndHorizontal();

            if(launchClient) StartAsClient(relayManualJoinCode);
        }
        else
        {
            GUILayout.Space(50);
            if (GUILayout.Button("               Close               "))
            {
                Destroy(_connection);
                _connection = null;
            }
        }
    }

    #endregion
}
