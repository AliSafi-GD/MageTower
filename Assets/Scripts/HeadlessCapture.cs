using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Window-free run-and-observe capture:
//   Game.exe -batchmode -screen-width 1280 -screen-height 720 --headless-capture C:/abs/shot.png --seconds 9
// Renders the main camera into a RenderTexture instead of the screen, so no window is shown.
// Does nothing unless the player was launched with --headless-capture.
public class HeadlessCapture : MonoBehaviour
{
    RenderTexture _target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Arg("--headless-capture") == null) return;
        var go = new GameObject(nameof(HeadlessCapture));
        DontDestroyOnLoad(go);
        go.AddComponent<HeadlessCapture>();
    }

    static string Arg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        float seconds = float.TryParse(Arg("--seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : 9f;
        int width = int.TryParse(Arg("--width"), out var w) ? w : 1280;
        int height = int.TryParse(Arg("--height"), out var h) ? h : 720;

        // Let the scene bootstrap create its camera and HUD first.
        yield return null;

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("HeadlessCapture: no main camera found");
            Application.Quit(1);
            yield break;
        }

        _target = new RenderTexture(width, height, 24);
        cam.targetTexture = _target;

        // Screen-space overlay canvases draw to the screen, never into a camera target.
        // Route them through the camera so the HUD appears in the capture.
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = cam.nearClipPlane + 1f;
        }

        // Optional burst: --count N frames, --interval seconds apart; files get _00, _01, ... suffixes.
        int count = int.TryParse(Arg("--count"), out var c) ? Mathf.Max(1, c) : 1;
        float interval = float.TryParse(Arg("--interval"), NumberStyles.Float, CultureInfo.InvariantCulture, out var iv) ? Mathf.Max(0f, iv) : 0.1f;

        yield return new WaitForSeconds(seconds);

        var path = Arg("--headless-capture");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        for (int i = 0; i < count; i++)
        {
            if (i > 0 && interval > 0f) yield return new WaitForSeconds(interval);
            yield return new WaitForEndOfFrame();
            Capture(cam, image, width, height);
            var file = count == 1
                ? path
                : Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_" + i.ToString("00") + Path.GetExtension(path));
            File.WriteAllBytes(file, image.EncodeToPNG());
            Debug.Log("HeadlessCapture: wrote " + file);
        }
        Application.Quit(0);
    }

    void Capture(Camera cam, Texture2D image, int width, int height)
    {
        // A -batchmode player skips camera rendering in its loop, so render this frame explicitly.
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = _target };
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);
        else
            Debug.LogWarning("HeadlessCapture: render request not supported; capture may be blank");

        var previous = RenderTexture.active;
        RenderTexture.active = _target;
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
    }
}
