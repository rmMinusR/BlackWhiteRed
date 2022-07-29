using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class NotificationFeed : MonoBehaviour
{
    [TestButton("Test kill message", nameof(__TestKillMessage), isActiveAtRuntime = true, isActiveInEditor = false, order = 100)]
    [TestButton("Spam kill message", nameof(__SpamKillMessage), isActiveAtRuntime = true, isActiveInEditor = false, order = 101)]
    [TestButton("Test close"       , nameof(__TestClose      ), isActiveAtRuntime = true, isActiveInEditor = false, order = 200)]
    [TestButton("Test close all"   , nameof(__TestClear      ), isActiveAtRuntime = true, isActiveInEditor = false, order = 202)]
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
    private void __TestKillMessage()
    {
        Add()
            .Background(false)
            .WithPlayer(testKiller)
            .WithIcon(null)
            .WithPlayer(testKilled)
            .PrettyOpen();
    }

    private void __SpamKillMessage()
    {
        for (int i = 0; i < 5; ++i) __TestKillMessage();
    }

    private void __TestClose()
    {
        entries[0].PrettyClose();
    }

    private void __TestClear() => Clear(true);

    #endregion

    public Notification Add()
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
