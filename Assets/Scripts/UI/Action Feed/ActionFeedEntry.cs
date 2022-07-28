using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ActionFeedEntry : MonoBehaviour
{
    [Header("Content prefabs")]
    [SerializeField] private ActionFeedIcon iconPrefab;
    [SerializeField] private NameDisplayPlate namePrefab;

    [Header("Content")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject background;
    [SerializeField] [InspectorReadOnly] private ActionFeed feed;
    [SerializeField] [InspectorReadOnly] private List<GameObject> content;

    [Space]
    [SerializeField] [InspectorReadOnly] private float timeToLive;

    internal void StartBuilding(ActionFeed feed, float timeToLive)
    {
        this.feed = feed;
        Background(false);
        this.timeToLive = timeToLive;
        PrettyOpen();
    }

    private IEnumerator Start()
    {
        //Functions similar to .Build in typical Builder pattern

        //First, refresh contents
        foreach (GameObject o in content) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)o.transform);
        yield return null;

        //Then refresh self once content dimensions are known
        LayoutRebuilder.MarkLayoutForRebuild(contentRoot);
        yield return null;

        //Then refresh parent once own dimensions are known
        LayoutRebuilder.MarkLayoutForRebuild(feed.ContentRoot);
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

    private void Update()
    {
        timeToLive -= Time.deltaTime;
        if (timeToLive < 0) PrettyClose();
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
