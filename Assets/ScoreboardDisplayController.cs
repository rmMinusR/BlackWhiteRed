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

        int dotsPerTeam = scoreToWin - 1;
        float distancePerAnchor = 1.0f / scoreToWin * 0.5f;

        for(int i = 1; i <= dotsPerTeam; i++)
        {
            RectTransform positiveObj = Instantiate(dottedLinePrefab,dottedLineParentPanel).GetComponent<RectTransform>();
            positiveObj.anchorMin = new Vector2(0.5f + (distancePerAnchor * i), positiveObj.anchorMin.y);
            positiveObj.anchorMax = new Vector2(0.5f + (distancePerAnchor * i), positiveObj.anchorMax.y);
            RectTransform negativeObj = Instantiate(dottedLinePrefab, dottedLineParentPanel).GetComponent<RectTransform>();
            negativeObj.anchorMin = new Vector2(0.5f - (distancePerAnchor * i), negativeObj.anchorMin.y);
            negativeObj.anchorMax = new Vector2(0.5f - (distancePerAnchor * i), negativeObj.anchorMax.y);
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
        teamUi.fillBar.fillAmount = 1.0f * teamScore / scoreToWin;

    }
}
