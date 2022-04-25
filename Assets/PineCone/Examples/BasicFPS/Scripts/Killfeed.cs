using System.Collections.Generic;
using UnityEngine;

public class Killfeed : MonoBehaviour
{
    [SerializeField] private KillfeedElement killfeedElement;

    public List<KillfeedElement> killfeeds = new List<KillfeedElement>();

    public void AddKillfeed(PlayerColor player1, PlayerColor player2, bool isWorld = false)
    {
        if (killfeeds.Count >= 5)
        {
            Destroy(killfeeds[0].gameObject);
            killfeeds.RemoveAt(0);
        }

        KillfeedElement element = Instantiate(killfeedElement, transform);
        if (!isWorld)
            element.SetText(this, player1, player2);
        else
            element.SetTextWorld(this, player1);
        killfeeds.Add(element);
    }
}
