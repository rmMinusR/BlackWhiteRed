using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class NotificationFeed : MonoBehaviour
{
    [SerializeField] private Notification entryPrefab;

    [Space]
    [SerializeField] [Min(0.5f)] private float entryDisplayTime = 2.5f;
    [SerializeField] private RectTransform contentRoot;
    internal RectTransform ContentRoot => contentRoot;
    [SerializeField] private VerticalLayoutGroup layout;
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

    #region Death message

    public void ShowDeathMessage(PlayerController killer, PlayerController killed, DamageSource killSource)
    {
        Notification notif = Add().Background(killer != null && killer.OwnerClientId == NetworkManager.Singleton.LocalClientId);
        if (killer != null) notif.WithPlayer(killer);

        Debug.Log($"Received death message: {killer}->{killed} by {killSource}");

        //Write relevant icons
        if (killSource.HasFlag(DamageSource.SWORD    )) notif.WithIcon(swordKillIcon);
        if (killSource.HasFlag(DamageSource.ARROW    )) notif.WithIcon(  bowKillIcon);
        if (killSource.HasFlag(DamageSource.ABYSS    )) notif.WithIcon(  fellOffIcon);
        if (killSource.HasFlag(DamageSource.EXPLOSION)) notif.WithIcon(explosionIcon);

        notif.WithPlayer(killed);
    }

    #endregion

    #region Scored message

    public void ShowScoredMessage(PlayerController whoScored)
    {
        Add()
            .Background(whoScored.OwnerClientId == NetworkManager.Singleton.LocalClientId)
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

        MatchManager.clientside_onTeamScore -= ShowScoredMessage;
        MatchManager.clientside_onTeamScore += ShowScoredMessage;
    }

    private void OnDisable()
    {
        Instance = null;

        MatchManager.clientside_onTeamScore -= ShowScoredMessage;
    }
}
