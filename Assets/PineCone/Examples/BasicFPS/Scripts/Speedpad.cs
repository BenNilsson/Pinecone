using UnityEngine;

namespace Pinecone.Examples.BasicFPS
{
    public class Speedpad : MonoBehaviour
    {
        [SerializeField] private float speedMultiplier = 10.0f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.name.Contains("FpsPlayer"))
                return;

            other.gameObject.GetComponent<PlayerController>()?.SetSpeedMultiplier(speedMultiplier);
        }
    }
}
