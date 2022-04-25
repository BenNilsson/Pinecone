using System.Collections.Generic;
using UnityEngine;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private ScoreboardElement prefab;
    [SerializeField] private Transform content;
    public Dictionary<string, ScoreboardElement> scoreboardElements = new Dictionary<string, ScoreboardElement>();
    [SerializeField] private CanvasGroup canvasGroup;

    public void AddPlayer(Player player)
    {
        ScoreboardElement scoreboardElement = Instantiate(prefab, content);
        scoreboardElement.SetPlayer(player);
        scoreboardElements.Add(player.playerColor.colorName, scoreboardElement);
    }

    public void RemovePlayer(string colorName)
    {
        if (scoreboardElements.TryGetValue(colorName, out ScoreboardElement element))
        {
            Destroy(element.gameObject);
            scoreboardElements.Remove(colorName);
        }
    }

    public void Display()
    {
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
    }
}
