using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreboardDisplayController : MonoBehaviour
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


    void Start()
    {
        Init();
    }

    private void Init()
    {
        scoreToWinMessageTextDisplay.text = "Score " + scoreToWin + " to Win!";
        MakeDottedLines();
        RefreshScoreDisplay();
    }

    private void MakeDottedLines()
    {
        //Reset Children
        while(dottedLineParentPanel.childCount > 0)
        {
            Destroy(dottedLineParentPanel.GetChild(0));
        }

        int dotsTotal = scoreToWin * 2 - 2;
        float distancePerAnchor = 1.0f / (dotsTotal+1);

        for (int i = 1; i <= dotsTotal; i++)
        {
            RectTransform dottedLineObj = Instantiate(dottedLinePrefab,dottedLineParentPanel).GetComponent<RectTransform>();
            dottedLineObj.anchorMin = new Vector2(distancePerAnchor * i, dottedLineObj.anchorMin.y);
            dottedLineObj.anchorMax = new Vector2(distancePerAnchor * i, dottedLineObj.anchorMax.y);
        }
    }

    private void RefreshScoreDisplay()
    {
        RefreshTeamScoreDisplay(blackTeam, blackScore);
        RefreshTeamScoreDisplay(whiteTeam, whiteScore);
    }

    private void RefreshTeamScoreDisplay(TeamUiItems teamUi, int teamScore)
    {
        teamUi.scoreTextDisplay.text = "" + teamScore;
        teamUi.fillBar.fillAmount = 1.0f * teamScore / (scoreToWin * 2 - 1);

    }
}
