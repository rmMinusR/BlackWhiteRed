using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnnouncementBannerDriver : MonoBehaviour
{
    #region Public interface functions

    public void Show(string text, float duration) => queue.Add(new Entry { text = text, duration = duration });
    public void Close() => timeUntilClose = 0;
    public void Clear() => queue.Clear();

    #endregion

    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text textTarget;
    [SerializeField] private float padding;
    [SerializeField] private AnimationCurve animOpen  = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve animClose = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Space]
    [SerializeField] private List<Entry> queue;
    [SerializeField] [InspectorReadOnly] private State state = State.Closed;
    [SerializeField] [InspectorReadOnly] private float timeUntilClose = 0;
    [SerializeField] [InspectorReadOnly] private float animationTime = 0;

    [Flags] public enum State
    {
        Animating = 0b10,
        Closed  = 0b00,
        Open    = 0b01,

        Closing = Closed | Animating,
        Opening = Open   | Animating,
    }

    [Serializable]
    public struct Entry
    {
        [Min(0)] public float duration;
        public string text;
    }

    private void Start()
    {
        //Start closed
        contentRoot.sizeDelta = new Vector2(0, contentRoot.sizeDelta.y);
    }

    void Update()
    {
        contentRoot.gameObject.SetActive(state != State.Closed);

        //Tick animation
        if (state.HasFlag(State.Animating))
        {
            AnimationCurve anim = state switch
            {
                State.Opening => animOpen,
                State.Closing => animClose,
                _ => throw new NotImplementedException(),
            };
            animationTime += Time.deltaTime;

            //Write to transform
            contentRoot.sizeDelta = new Vector2(
                (textTarget.rectTransform.sizeDelta.x + 2*padding) * anim.Evaluate(animationTime),
                contentRoot.sizeDelta.y
            );

            //Mark if done animating
            if (animationTime >= anim.keys[anim.length-1].time) state &= ~State.Animating;
        }

        //Close after a period of time being open
        if (state == State.Open)
        {
            timeUntilClose -= Time.deltaTime;
            if (timeUntilClose <= 0)
            {
                state = State.Closing;
                animationTime = 0;
            }
        }

        //If closed, try to open next entry in queue
        if (state == State.Closed && queue.Count > 0)
        {
            textTarget.text = queue[0].text;
            timeUntilClose = queue[0].duration;
            queue.RemoveAt(0);

            state = State.Opening;
            animationTime = 0;
        }
    }
}
