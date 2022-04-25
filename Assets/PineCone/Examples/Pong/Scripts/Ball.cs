using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pinecone;

[RequireComponent(typeof(Rigidbody2D))]
public partial class Ball : NetworkBehaviour
{
    [SerializeField] private float speed = 30;

    private Rigidbody2D rigidbody2d;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    public override void OnStart()
    {
        if (HasAuthority)
            rigidbody2d.simulated = true;
    }

    /// <summary>
    /// Function called by the server when both players have connected or when a new round starts.
    /// </summary>
    public void StartMovingBall()
    {
        rigidbody2d.velocity = (Random.Range(0, 2) == 0 ? Vector3.right : Vector3.left) * speed;
    }

    private void OnCollisionEnter2D(Collision2D colllider)
    {
        if (!NetworkServer.IsActive)
            return;

        if (colllider.gameObject.name.Contains("Bat"))
        {
            Vector2 direction = new Vector2();
            direction.x = colllider.relativeVelocity.x > 0 ? 1 : -1;
            direction.y = (transform.position.y - colllider.transform.position.y) / colllider.collider.bounds.size.y;
            rigidbody2d.velocity = direction.normalized * speed;
        }
    }
}
