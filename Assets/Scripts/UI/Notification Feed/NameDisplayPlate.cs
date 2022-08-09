using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NameDisplayPlate : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text text;

    [Space]
    [SerializeField] private Color whiteTeamFg = Color.black;
    [SerializeField] private Color whiteTeamBg = Color.white;

    [Space]
    [SerializeField] private Color blackTeamFg = Color.white;
    [SerializeField] private Color blackTeamBg = Color.black;

    public void Write(string text, Color foreground, Color background)
    {
        this.background.color = background;
        this.text.color = foreground;
        this.text.text = text;
    }

    public void Write(PlayerController player)
    {
        if (player.CurrentTeam == Team.INVALID) throw new InvalidOperationException("Must assign a team to player first!");

        Write(
            player.name, //FIXME placeholder, use player's display name instead
            player.CurrentTeam == Team.BLACK ? blackTeamFg : whiteTeamFg,
            player.CurrentTeam == Team.BLACK ? blackTeamBg : whiteTeamBg
        );
    }
}
