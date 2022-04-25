using UnityEngine;
using Pinecone;

public partial class Goal : NetworkBehaviour
{
    [SerializeField] private int goalIndex;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Server only :D
        if (!NetworkServer.IsActive)
            return;

        // Check name of object to avoid having to add tags/layers. Bad for examples (:
        if (collision.gameObject == null || !collision.gameObject.name.Contains("Ball"))
            return;

        GameManager.TriggerOnGoal(goalIndex);
    }
}
