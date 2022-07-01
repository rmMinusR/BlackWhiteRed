using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderScoreboardDisplayController : MonoBehaviour
{
    [System.Serializable]
    struct TeamUiItems
    {
        public Image fillBar;
        public TextMeshProUGUI scoreTextDisplay;
    }

    [SerializeField]
    TeamUiItems blackTeam;
    [SerializeField]
    TeamUiItems whiteTeam;

    [SerializeField]
    TextMeshProUGUI scoreToWinMessageTextDisplay;
    [SerializeField]
    Transform dottedLineParentPanel;
    [SerializeField]
    GameObject dottedLinePrefab;

    [Space]
    [SerializeField]
    [Min(1)]
    int scoreToWin;
    [SerializeField]
    [Min(0)]
    int blackScore;
    [SerializeField]
    [Min(0)]
    int whiteScore;

    [Space]
    [Header("Animation Timings")]
    [SerializeField]
    float timeToFill = 0.5f;

    class FillLerpInfo
    {
        public float oldFillAmount;
        public Image fillImage;
        public float newFillAmount;
        public float fillTimer;
    }

    List<FillLerpInfo> fillsPerforming;

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        HandleFillLerp();
    }

    private void Init()
    {
        fillsPerforming = new List<FillLerpInfo>();
        scoreToWinMessageTextDisplay.text = "Score " + scoreToWin + " to Win!";
        MakeDottedLines();
        RefreshScoreDisplay();
    }

    private void MakeDottedLines()
    {
        int dotsTotal = scoreToWin * 2 - 2;
        float distancePerAnchor = 1.0f / (dotsTotal+1);

        for (int i = 1; i <= dotsTotal; i++)
        {
            RectTransform dottedLineObj = Instantiate(dottedLinePrefab,dottedLineParentPanel).GetComponent<RectTransform>();
            dottedLineObj.anchorMin = new Vector2(distancePerAnchor * i, dottedLineObj.anchorMin.y);
            dottedLineObj.anchorMax = new Vector2(distancePerAnchor * i, dottedLineObj.anchorMax.y);
        }
    }

    [ContextMenu("Refresh Fills")]
    private void RefreshScoreDisplay()
    {
        RefreshTeamScoreDisplay(blackTeam, blackScore);
        RefreshTeamScoreDisplay(whiteTeam, whiteScore);
    }

    private void RefreshTeamScoreDisplay(TeamUiItems teamUi, int teamScore)
    {
        teamUi.scoreTextDisplay.text = "" + teamScore;

        //teamUi.fillBar.fillAmount = 1.0f * teamScore / (scoreToWin * 2 - 1);
        FillLerpInfo temp = new FillLerpInfo();
        temp.oldFillAmount = teamUi.fillBar.fillAmount;
        temp.newFillAmount= 1.0f * teamScore / (scoreToWin * 2 - 1);
        temp.fillImage = teamUi.fillBar;
        temp.fillTimer = timeToFill;

        fillsPerforming.Add(temp);
    }

    private void HandleFillLerp()
    {
        for (int i = fillsPerforming.Count - 1; i >= 0; i--)
        {
            if (fillsPerforming[i].fillTimer > 0)
            {
                fillsPerforming[i].fillTimer -= Time.deltaTime;

                if (fillsPerforming[i].fillTimer <= 0)
                {
                    fillsPerforming[i].fillImage.fillAmount = fillsPerforming[i].newFillAmount;
                    fillsPerforming.RemoveAt(i);
                }
                else
                {
                    fillsPerforming[i].fillImage.fillAmount = Mathf.Lerp(fillsPerforming[i].newFillAmount, fillsPerforming[i].oldFillAmount, fillsPerforming[i].fillTimer / timeToFill);
                }
            }
        }
    }
}
