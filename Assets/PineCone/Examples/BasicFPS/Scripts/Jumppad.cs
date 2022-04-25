using UnityEngine;

public class Jumppad : MonoBehaviour
{
    [SerializeField] private float jumpPower = 20;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.name.Contains("FpsPlayer"))
            return;

        other.gameObject.GetComponent<PlayerController>()?.AddJumpHeight(jumpPower);
    }
}
