using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.ComponentModel;
using System;

public class ScoreboardElement : MonoBehaviour
{
    public TextMeshProUGUI playerName;
    public Color32 color;
    public Image playerIcon;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI deathsText;
    public Player player;

    public void SetPlayer(Player player)
    {
        this.player = player;
        playerName.text = player.playerColor.colorName;
        color = player.playerColor.color;
        playerIcon.color = color;
        playerName.faceColor = color;
        killsText.text = player.Kills.ToString();
        deathsText.text = player.Deaths.ToString();

        player.OnSyncVarValueChanged += PropertyChanged;
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnSyncVarValueChanged -= PropertyChanged;
    }

    private void PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Kills")
            killsText.text = player.Kills.ToString();
        else if (e.PropertyName == "Deaths")
            deathsText.text = player.Deaths.ToString();
    }
}
