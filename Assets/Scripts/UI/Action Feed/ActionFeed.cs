using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class ActionFeed : MonoBehaviour
{
    [TestButton("Test kill message", nameof(__TestKillMessage))]
    [TestButton("Test close", nameof(__TestClose))]
    [SerializeField] private ActionFeedEntry entryPrefab;

    [Space]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private List<ActionFeedEntry> entries;

    private void __TestKillMessage()
    {
        Add()
            .Background(false)
            .WithPlayer(FindObjectOfType<PlayerController>())
            .WithIcon(null)
            .WithPlayer(FindObjectOfType<PlayerController>())
            .PrettyOpen();
    }

    private void __TestClose()
    {
        entries[0].PrettyClose();
    }

    public ActionFeedEntry Add()
    {
        ActionFeedEntry e = Instantiate(entryPrefab.gameObject, contentRoot).GetComponent<ActionFeedEntry>();

        e.StartBuilding(this);

        entries.Add(e);
        return e;
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
        }
    }
}
