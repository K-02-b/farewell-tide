using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TideTitle : MonoBehaviour
{
    static bool endingComplete;
    public SpriteRenderer background;
    RectTransform hint;
    AudioSource menuAudio;
    AudioClip confirm;
    Camera view;
    bool entering;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEndingBackground() => endingComplete = false;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RestoreWindowFrame()
    {
        // Existing players can retain fullscreen preferences from an earlier build.
        if (Screen.fullScreenMode == FullScreenMode.Windowed) return;
        float scale = Mathf.Min(1f, Display.main.systemWidth * .9f / 1280f,
            Display.main.systemHeight * .9f / 720f);
        Screen.SetResolution(Mathf.RoundToInt(1280 * scale), Mathf.RoundToInt(720 * scale),
            FullScreenMode.Windowed);
    }
#endif

    public static void MarkEndingComplete() => endingComplete = true;

    void Start()
    {
        if (endingComplete)
        {
            var endingBackground = TideArt.Pic("Ending/AfterEndTitle");
            if (background && endingBackground)
            {
                TideArt.Fit(background, endingBackground, 9);
                // The opening art already contains the lettering. Its transparent source is cropped to the left edge.
                var lettering = TideArt.Pic("Ending/FinalTitle");
                if (lettering)
                {
                    var title = TideArt.Place("结局主页标题", lettering, 0, 0, background.bounds.size.y, background.sortingOrder + 1);
                    title.sortingLayerID = background.sortingLayerID;
                    title.transform.position += new Vector3(background.bounds.min.x - title.bounds.min.x,
                        background.bounds.max.y - title.bounds.max.y, 0);
                }
            }
        }
        view = Camera.main;
        var hud = new TideHUD();
        menuAudio=gameObject.AddComponent<AudioSource>();menuAudio.playOnAwake=false;
        confirm=Resources.Load<AudioClip>("Tide/Sound/Pickup");
        hint = hud.Panel("首页提示", .5f, 0f, 0f, 20f, 360f, 36f, Color.clear);
        var text = hud.Caption(hint, "> 点击即可开始游戏 <", 22, TextAnchor.MiddleCenter);
        text.supportRichText = false;
        text.color = new Color32(255, 219, 158, 255);
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(60, 39, 26, 220);
        outline.effectDistance = new Vector2(1.3f, -1.3f);
        Layout();
    }

    void Update()
    {
        Layout();
        if (entering || TidePause.BlocksInput || !Input.GetMouseButtonDown(0)) return;
        if (!view.pixelRect.Contains(Input.mousePosition)) return;
        entering = true;
        StartCoroutine(StartGame());
    }

    void Layout()
    {
        TideArt.CameraFrame(view, 16, 9);
        hint.anchorMin = hint.anchorMax = new Vector2(.5f, view.rect.yMin);
    }

    IEnumerator StartGame()
    {
        if (confirm)
        {
            menuAudio.PlayOneShot(confirm, .5f);
            yield return new WaitForSeconds(Mathf.Min(.30f, confirm.length));
        }
        TideTimeline.BeginRun();
        endingComplete = false;
        SceneManager.LoadScene("01_Cottage");
    }
}
