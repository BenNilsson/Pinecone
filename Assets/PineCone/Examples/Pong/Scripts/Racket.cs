using UnityEngine;
using Pinecone;
using System;

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
        PongNetworkManager.OnGoalScored += ResetPosition;
    }

    private void OnDestroy()
    {
        if (!HasAuthority)
            return;

        PongNetworkManager.OnGoalScored -= ResetPosition;
    }

    private void ResetPosition()
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
