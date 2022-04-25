using UnityEngine;
using Pinecone;

public partial class Racket : NetworkBehaviour
{
    [SerializeField] private float speed = 8;
    [SerializeField] private Rigidbody2D rigidbody2d;

    private Vector3 defaultPosition;

    public override void OnStart()
    {
        if (!HasAuthority)
            return;

        defaultPosition = transform.position;
        GameManager.OnGoal += ResetPosition;
    }

    private void OnDestroy()
    {
        if (!HasAuthority)
            return;

        GameManager.OnGoal -= ResetPosition;
    }

    private void ResetPosition(int goalIndex)
    {
        transform.position = defaultPosition;
    }

    private void FixedUpdate()
    {
        if (HasAuthority)
        {
            rigidbody2d.velocity = new Vector2(0, Input.GetAxisRaw("Vertical")) * speed;
        }
    }
}
