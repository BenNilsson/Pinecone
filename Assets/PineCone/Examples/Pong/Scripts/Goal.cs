using UnityEngine;

namespace Pinecone.Examples.Pong
{
    public partial class Goal : NetworkBehaviour
    {
        [SerializeField] private int goalIndex;
        [SerializeField] private GameLogic gameLogic;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Server only :D
            if (!NetworkServer.IsActive)
                return;

            // Check name of object to avoid having to add tags/layers. Bad for examples (:
            if (collision.gameObject == null || !collision.gameObject.name.Contains("Ball"))
                return;

            gameLogic.ServerIncrementScore(goalIndex);
        }
    }
}
