using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DottedScoreboardDisplayController : MonoBehaviour
{
    [System.Serializable]
    struct ScoreDotsData
    {
        public GameObject dotPrefab;
        public Transform panelParent;
        [HideInInspector]
        public DotPointController[] dots;
    }

    [SerializeField]
    ScoreDotsData black;
    [SerializeField]
    ScoreDotsData white;
    [Space]
    [SerializeField]
    [Min(1)]
    int scoreToWin;
    [SerializeField]
    [TestButton("Black Scores!","ScoreBlack")]
    [Min(0)]
    int blackScore;
    [SerializeField]
    [TestButton("White Scores!","ScoreWhite")]
    [Min(0)]
    int whiteScore;

    private void OnEnable()
    {
        MatchManager.onMatchStart += HandleMatchStart;
        MatchManager.onTeamScore += HandleTeamScore;
        MatchManager.onTeamWin += HandleTeamScore;
    }

    private void OnDisable()
    {
        MatchManager.onMatchStart -= HandleMatchStart;
        MatchManager.onTeamScore -= HandleTeamScore;
        MatchManager.onTeamWin -= HandleTeamScore;
    }

    private void HandleTeamScore(Team team)
    {
        if(team == Team.BLACK)
        {
            ScoreBlack();
        }
        else
        {
            ScoreWhite();
        }
    }

    private void HandleMatchStart()
    {
        //Set the correct values
        scoreToWin = MatchManager.Instance.scoreToWin;
        blackScore = 0;
        whiteScore = 0;

        //Initialize the display
        Init();
    }

    private void Init()
    {
        InitTeamPanel(ref black);
        InitTeamPanel(ref white);
    }

    private void InitTeamPanel(ref ScoreDotsData data)
    {
        data.dots = new DotPointController[scoreToWin];
        for(int i = 0; i < scoreToWin; i++)
        {
            data.dots[i] = Instantiate(data.dotPrefab, data.panelParent).GetComponent<DotPointController>();
        }
    }

    private void ScoreBlack()
    {
        ScoreTeam(ref black, ref blackScore);
    }

    private void ScoreWhite()
    {
        ScoreTeam(ref white, ref whiteScore);
    }

    private void ScoreTeam(ref ScoreDotsData data, ref int score)
    {
        if (score < scoreToWin)
        {
            data.dots[score].MarkOff();
            score++;
        }
    }

}
