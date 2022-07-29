using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class NotificationFeed : NetworkBehaviour
{
    [SerializeField] private Notification entryPrefab;

    [Space]
    [SerializeField] [Min(0.5f)] private float entryDisplayTime = 2.5f;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private VerticalLayoutGroup layout;
    internal RectTransform ContentRoot => contentRoot;
    [SerializeField] private List<Notification> entries;

    private (float, int) __maxDisplay; //Flyweight
    private int MaxDisplayCount
    {
        get
        {
            float ownHeight = ((RectTransform)transform).rect.height;
            if (__maxDisplay.Item1 == ownHeight) return __maxDisplay.Item2;

            float availableHeight = ownHeight + layout.padding.top + layout.padding.bottom;
            float heightPerEntry = layout.spacing + ((RectTransform)entryPrefab.transform).rect.height;
            int c = (int)( (availableHeight+layout.spacing) / heightPerEntry );

            __maxDisplay = (ownHeight, c);
            return c;
        }
    }

    #region Notification type definitions

    [Header("Icons")]
    [SerializeField] private Sprite swordKillIcon;
    [SerializeField] private Sprite   bowKillIcon;
    [SerializeField] private Sprite   fellOffIcon;
    [SerializeField] private Sprite explosionIcon;
    [SerializeField] private Sprite    scoredIcon;

    //Helpers
    private void EnsureHasSpawnPermission()
    {
        if (NetworkManager == null || !(NetworkManager.IsServer || NetworkManager.IsClient)) throw new InvalidOperationException("Not connected!");
        if (!IsServer) throw new AccessViolationException("Only server may send notifications!");
    }

    private const ulong NULL_PLAYER_ID = ulong.MaxValue;
    private static ulong TxPlayer(PlayerController c, bool require = true) => require||c!=null ? c.OwnerClientId : NULL_PLAYER_ID;
    private static PlayerController RxPlayer(ulong id, bool require = true) => require||id!=NULL_PLAYER_ID ? NetHeartbeat.Of(id).GetComponent<PlayerController>() : null;

    #region Death message

    public void BroadcastDeathMessage(PlayerController killer, PlayerController killed, DamageSource killSource)
    {
        EnsureHasSpawnPermission();
        DONOTCALL_ShowDeathMessage_ClientRpc(TxPlayer(killer, require: false), TxPlayer(killed), killSource); //Broadcast to all
    }

    [ClientRpc]
    private void DONOTCALL_ShowDeathMessage_ClientRpc(ulong killerID, ulong killedID, DamageSource killSource, ClientRpcParams p = default)
    {
        PlayerController killer = RxPlayer(killerID, require: false);
        PlayerController killed = RxPlayer(killedID);

        Notification notif = Add().Background(killerID == NetworkManager.LocalClientId);
        if (killer != null) notif.WithPlayer(killer);

        //Write relevant icons
        if (killSource.HasFlag(DamageSource.SWORD    )) notif.WithIcon(swordKillIcon);
        if (killSource.HasFlag(DamageSource.ARROW    )) notif.WithIcon(  bowKillIcon);
        if (killSource.HasFlag(DamageSource.ABYSS    )) notif.WithIcon(  fellOffIcon);
        if (killSource.HasFlag(DamageSource.EXPLOSION)) notif.WithIcon(explosionIcon);

        notif.WithPlayer(killed);
    }

    #endregion

    #region Scored message

    public void BroadcastScoredMessage(PlayerController whoScored)
    {
        EnsureHasSpawnPermission();
        DONOTCALL_ShowScoredMessage_ClientRpc(TxPlayer(whoScored)); //Broadcast to all
    }

    [ClientRpc]
    private void DONOTCALL_ShowScoredMessage_ClientRpc(ulong whoScoredID, ClientRpcParams p = default)
    {
        PlayerController whoScored = RxPlayer(whoScoredID);
        
        Add()
            .Background(whoScoredID == NetworkManager.LocalClientId)
            .WithPlayer(whoScored)
            .WithIcon(scoredIcon);
    }

    #endregion

    #endregion

    #region Builders/helpers

    private Notification Add() //Would be public, but we need to make sure Notifications are only spawned through the Show commands
    {
        //OverflowCheck();

        Notification e = Instantiate(entryPrefab.gameObject, contentRoot).GetComponent<Notification>();
        e.StartBuilding(this, entryDisplayTime);
        entries.Add(e);
        return e;
    }

    private void Update() => OverflowCheck();

    private void OverflowCheck()
    {
        int toRemove = Mathf.Max(entries.Count-MaxDisplayCount, 0);
        for (int i = 0; i < toRemove; ++i) entries[i].PrettyClose();
    }

    internal void Remove(Notification e) => entries.Remove(e);

    public void Clear(bool pretty)
    {
        if (pretty)
        {
            foreach (Notification e in entries) e.PrettyClose();
        }
        else
        {
            foreach (Notification e in entries) Destroy(e.gameObject);
            entries.Clear();
        }
    }

    #endregion

    public static NotificationFeed Instance { get; private set; }

    private void OnEnable()
    {
        Instance = this;

        MatchManager.serverside_onScore -= BroadcastScoredMessage;
        MatchManager.serverside_onScore += BroadcastScoredMessage;
    }

    private void OnDisable()
    {
        Instance = null;

        MatchManager.serverside_onScore -= BroadcastScoredMessage;
    }
}
