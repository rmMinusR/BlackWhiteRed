using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class NotificationFeed : NetworkBehaviour
{
    [TestButton("Test kill message"  , nameof(__TestKillMessage  ), isActiveAtRuntime = true, isActiveInEditor = false, order = 100)]
    [TestButton("Spam kill message"  , nameof(__SpamKillMessage  ), isActiveAtRuntime = true, isActiveInEditor = false, order = 101)]
    [TestButton("Test scored message", nameof(__TestScoredMessage), isActiveAtRuntime = true, isActiveInEditor = false, order = 120)]
    [TestButton("Test close"         , nameof(__TestClose        ), isActiveAtRuntime = true, isActiveInEditor = false, order = 200)]
    [TestButton("Test close all"     , nameof(__TestClear        ), isActiveAtRuntime = true, isActiveInEditor = false, order = 202)]
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

    #region TESTING ONLY

    [Header("TESTING")]
    [SerializeField] private PlayerController testKiller;
    [SerializeField] private PlayerController testKilled;
    [SerializeField] private KillSource testKillSource;
    private void __TestKillMessage() => ShowDeathMessage(testKiller, testKilled, testKillSource);
    private void __SpamKillMessage() { for (int i = 0; i < 5; ++i) __TestKillMessage(); }
    private void __TestScoredMessage() => ShowScoredMessage(testKiller);

    private void __TestClose() => entries[0].PrettyClose();
    private void __TestClear() => Clear(true);

    #endregion

    #region Notification type definitions

    [Header("Icons")]
    [SerializeField] private Sprite swordKillIcon;
    [SerializeField] private Sprite   bowKillIcon;
    [SerializeField] private Sprite   fellOffIcon;
    [SerializeField] private Sprite    scoredIcon;

    [Flags]
    public enum KillSource
    {
        Sword = (1 << 0),
        Bow   = (1 << 1),

        FellOff = (1 << 8)
    }

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

    public void ShowDeathMessage(PlayerController killer, PlayerController killed, KillSource killSource)
    {
        EnsureHasSpawnPermission();
        DONOTCALL_ShowDeathMessage_ClientRpc(TxPlayer(killer, require: false), TxPlayer(killed), killSource); //Broadcast to all
    }

    [ClientRpc]
    private void DONOTCALL_ShowDeathMessage_ClientRpc(ulong killerID, ulong killedID, KillSource killSource, ClientRpcParams p = default)
    {
        PlayerController killer = RxPlayer(killerID, require: false);
        PlayerController killed = RxPlayer(killedID);

        Notification notif = Add().Background(killerID == OwnerClientId);
        if (killer != null) notif.WithPlayer(killer);

        //Write relevant icons
        if (killSource.HasFlag(KillSource.Sword  )) notif.WithIcon(swordKillIcon);
        if (killSource.HasFlag(KillSource.Bow    )) notif.WithIcon(  bowKillIcon);
        if (killSource.HasFlag(KillSource.FellOff)) notif.WithIcon(  fellOffIcon);

        notif.WithPlayer(killed);
    }

    #endregion

    #region Scored message

    public void ShowScoredMessage(PlayerController whoScored)
    {
        EnsureHasSpawnPermission();
        DONOTCALL_ShowScoredMessage_ClientRpc(TxPlayer(whoScored)); //Broadcast to all
    }

    [ClientRpc]
    private void DONOTCALL_ShowScoredMessage_ClientRpc(ulong whoScoredID, ClientRpcParams p = default)
    {
        PlayerController whoScored = RxPlayer(whoScoredID);

        Add()
            .Background(whoScoredID == OwnerClientId)
            .WithPlayer(whoScored)
            .WithIcon(scoredIcon);
    }

    #endregion

    #endregion


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
}
