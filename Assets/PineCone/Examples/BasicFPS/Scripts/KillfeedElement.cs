using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Pinecone.Examples.BasicFPS
{
    public class KillfeedElement : MonoBehaviour
    {
        public TextMeshProUGUI player1;
        public Image deathImage;
        public TextMeshProUGUI player2;
        private Killfeed killfeed;

        [SerializeField] private Sprite worldIcon;

        public void SetText(Killfeed killfeed, PlayerColor player1, PlayerColor player2)
        {
            this.killfeed = killfeed;

            this.player1.faceColor = player1.color;
            this.player1.text = player1.colorName;

            this.player2.faceColor = player2.color;
            this.player2.text = player2.colorName;
        }

        public void SetTextWorld(Killfeed killfeed, PlayerColor player1)
        {
            this.killfeed = killfeed;

            this.player1.faceColor = player1.color;
            this.player1.text = player1.colorName;
            deathImage.sprite = worldIcon;
            this.player2.enabled = false;
        }

        private void Start()
        {
            Destroy(gameObject, 5f);
        }

        private void OnDestroy()
        {
            if (killfeed.killfeeds.Contains(this))
                killfeed.killfeeds.Remove(this);
        }
    }
}