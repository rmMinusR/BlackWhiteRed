using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class ActionFeed : MonoBehaviour
{
    [TestButton("Test kill message", nameof(__TestKillMessage), isActiveAtRuntime = true, isActiveInEditor = false, order = 100)]
    [TestButton("Spam kill message", nameof(__SpamKillMessage), isActiveAtRuntime = true, isActiveInEditor = false, order = 101)]
    [TestButton("Test close"       , nameof(__TestClose      ), isActiveAtRuntime = true, isActiveInEditor = false, order = 200)]
    [TestButton("Test close all"   , nameof(__TestClear      ), isActiveAtRuntime = true, isActiveInEditor = false, order = 202)]
    [SerializeField] private ActionFeedEntry entryPrefab;

    [Space]
    [SerializeField] [Min(0.5f)] private float entryDisplayTime = 2.5f;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private VerticalLayoutGroup layout;
    internal RectTransform ContentRoot => contentRoot;
    [SerializeField] private List<ActionFeedEntry> entries;

    //private int? __maxDisplayCount = null;
    private int MaxDisplayCount
    {
        get
        {
            //if (__maxDisplayCount.HasValue) return __maxDisplayCount.Value;

            float availableHeight = ((RectTransform)transform).rect.height + layout.padding.top + layout.padding.bottom;
            float heightPerEntry = layout.spacing + ((RectTransform)entryPrefab.transform).rect.height;
            int c = (int)( (availableHeight+layout.spacing) / heightPerEntry );

            //__maxDisplayCount = c;
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

    public ActionFeedEntry Add()
    {
        //OverflowCheck();

        ActionFeedEntry e = Instantiate(entryPrefab.gameObject, contentRoot).GetComponent<ActionFeedEntry>();
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

    internal void Remove(ActionFeedEntry e) => entries.Remove(e);

    public void Clear(bool pretty)
    {
        if (pretty)
        {
            foreach (ActionFeedEntry e in entries) e.PrettyClose();
        }
        else
        {
            foreach (ActionFeedEntry e in entries) Destroy(e.gameObject);
            entries.Clear();
        }
    }
}
