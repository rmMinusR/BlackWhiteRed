using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class SceneLoadMonitor : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private string loadTargetFormat = "Loading {0}";
    [SerializeField] private TMP_Text loadTargetText;
    [SerializeField] private string loadPercentFormat = "{0}%";
    [SerializeField] private TMP_Text loadPercentText;

    [Space]
    [SerializeField] private UnityAction onLoadBegin;
    [SerializeField] private UnityAction onLoadComplete;

    private SceneGroupLoader.LoadOp watching;

    public void Monitor(SceneGroupLoader.LoadOp progress)
    {
        watching = progress;
        onLoadBegin();
        watching.onComplete += Unmonitor;

        __UIUpdateWorkerInst = StartCoroutine(UIUpdateWorker());
    }

    private Coroutine __UIUpdateWorkerInst;
    private IEnumerator UIUpdateWorker()
    {
        while (watching != null)
        {
            if (progressBar     != null) progressBar.fillAmount = watching.Progress;
            if (loadTargetText  != null) loadTargetText .text = string.Format(loadTargetFormat , watching.CurrentlyLoading);
            if (loadPercentText != null) loadPercentText.text = string.Format(loadPercentFormat, (int)(watching.Progress * 100));

            yield return null;
        }
    }

    private void Unmonitor()
    {
        StopCoroutine(__UIUpdateWorkerInst);
        __UIUpdateWorkerInst = null;

        onLoadComplete();
        watching = null;
    }
}
