using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FreezeCam : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RawImage rawImage;

    private Texture2D tex;

    public void ShowFreezeCam()
    {
        StartCoroutine(TakeScreenShot());
    }

    private IEnumerator TakeScreenShot()
    {
        // Wait 1 frame in order for line renderers to actually display.
        yield return null;
        tex = RTImage();
        rawImage.texture = tex;
        rawImage.gameObject.SetActive(true);
    }

    public void HideFreezeCam()
    {
        rawImage.gameObject.SetActive(false);
    }

    private Texture2D RTImage()
    {
        int width = Screen.width;
        int height = Screen.height;

        Rect rect = new Rect(0, 0, width, height);
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);

        mainCamera.targetTexture = renderTexture;
        mainCamera.Render();

        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply();

        mainCamera.targetTexture = null;
        RenderTexture.active = null;

        Destroy(renderTexture);
        renderTexture = null;
        return screenShot;
    }
}
