using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnnouncementBannerDriver : MonoBehaviour
{
    //Public interface function
    public void Show(string text, float duration) => queue.Add(new Entry { text = text, duration = duration });

    [SerializeField] private Text textTarget;
    [SerializeField] private Animation animTarget;
    [SerializeField] private AnimationClip animOpen;
    [SerializeField] private AnimationClip animClose;

    [Space]
    [SerializeField] private List<Entry> queue;
    [SerializeField] [InspectorReadOnly] private State state = State.Closed;
    [SerializeField] [InspectorReadOnly] private float timeUntilClose = 0;

    [Flags] public enum State
    {
        Animating = 0b10,
        Closed  = 0b00,
        Open    = 0b01,

        Closing = Closed | Animating,
        Opening = Open   | Animating,
    }

    public struct Entry
    {
        [Min(0)] public float duration;
        public string text;
    }

    void Awake()
    {
        animTarget = GetComponent<Animation>();
    }

    void Update()
    {
        //Close after a period of time being open
        if (state == State.Open)
        {
            timeUntilClose -= Time.deltaTime;
            if (timeUntilClose <= 0) animTarget.Play(animClose.name);
        }

        //Mark if done animating
        if (state.HasFlag(State.Animating) && !animTarget.isPlaying) state &= ~State.Animating;

        //If closed, try to show next entry in queue
        if (state == State.Closed && queue.Count > 0)
        {
            textTarget.text = queue[0].text;
            timeUntilClose = queue[0].duration;
            animTarget.Play(animOpen.name);
        }
    }

    //Animation callbacks
    public void OnBeganOpening() => state = State.Opening;
    public void OnBeganClosing() => state = State.Closing;
    public void OnFinishedOpening() => state = State.Open;
    public void OnFinishedClosing() => state = State.Closed;
}
