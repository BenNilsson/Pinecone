using UnityEngine;

namespace Pinecone.Examples.BasicFPS
{
    public class MouseOnStart : MonoBehaviour
    {
        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}