using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActionFeedEntry : MonoBehaviour
{
    [Header("Content prefabs")]
    [SerializeField] private ActionFeedIcon iconPrefab;
    [SerializeField] private NameDisplayPlate namePrefab;

    [Header("Content")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] [InspectorReadOnly] private ActionFeed feed;
    [SerializeField] private GameObject background;


    internal void StartBuilding(ActionFeed feed)
    {
        this.feed = feed;
        Background(false);
    }

    public ActionFeedEntry WithIcon(Sprite icon)
    {
        ActionFeedIcon i = Instantiate(iconPrefab.gameObject, contentRoot).GetComponent<ActionFeedIcon>();
        i.icon.sprite = icon;

        return this;
    }

    public ActionFeedEntry WithPlayer(PlayerController player)
    {
        NameDisplayPlate n = Instantiate(namePrefab.gameObject, contentRoot).GetComponent<NameDisplayPlate>();
        n.Write(player);

        return this;
    }

    public ActionFeedEntry Background(bool enable)
    {
        background.SetActive(enable);

        return this;
    }

    private void OnDestroy()
    {
        feed.Remove(this);
    }

    [Header("Animations")]
    [SerializeField] private Animation animationTarget;
    [SerializeField] private AnimationClip openAnim;
    [SerializeField] private AnimationClip closeAnim;
    public void PrettyOpen() => animationTarget.Play(openAnim.name);
    public void PrettyClose() => animationTarget.Play(closeAnim.name);

    private void PrettyCloseFinish()
    {
        Destroy(gameObject);
    }
}
