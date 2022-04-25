using System.Collections;
using UnityEngine;
using Pinecone;
using UnityEngine.UI;

public partial class CameraController : NetworkBehaviour
{
    public Transform cameraPos;
    [SerializeField] private float mouseSensitivity = 1f;
    private float cameraPitch = 0.0f;

    public override void OnStart()
    {
        if (!HasAuthority)
        {
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void SetRotation(Quaternion rotation)
    {
        cameraPitch = 0f;
        transform.rotation = rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (!HasAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.visible)
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));


        cameraPitch -= mouseDelta.y * mouseSensitivity;

        cameraPitch = Mathf.Clamp(cameraPitch, -89.9f, 89.9f);

        cameraPos.localEulerAngles = Vector3.right * cameraPitch;

        transform.Rotate(Vector3.up * (mouseDelta.x * mouseSensitivity));
    }

    public void StartFreezeCam(Vector3 theirPos, FreezeCam freezeCam)
    {
        cameraPos.LookAt(theirPos + new Vector3(0, 1, 0));
        Physics.SyncTransforms();
        freezeCam.ShowFreezeCam();
    }


    public void FinishFreezeCam(FreezeCam freezeCam)
    {
        freezeCam.HideFreezeCam();
    }
}
