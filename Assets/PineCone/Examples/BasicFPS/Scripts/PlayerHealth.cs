using UnityEngine;
using Pinecone;

public partial class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100;
    private Player player;
    
    [NetworkSync]
    private int health;

    public bool Dead;

    public override void OnStart()
    {
        healthGenerated = maxHealth;
        player = GetComponent<Player>();
    }

    public void TakeDamage(int damage, string killedById)
    {
        if (!NetworkServer.IsActive)
            return;

        healthGenerated -= damage;
        if (health <= 0 && !Dead)
        {
            Dead = true;

            player.DeathsGenerated++;
            Generated.RpcDie(this, killedById);
        }
    }

    [NetworkRPC]
    public void RpcDie(string killedById)
    {
        player.Die(killedById);
    }

    public void SetHealth(int maxHealth)
    {
        healthGenerated = maxHealth;
    }
}
