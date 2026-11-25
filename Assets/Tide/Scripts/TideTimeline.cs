using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A single full-frame cue introduces each change of time. The pause canvas
// remains above it so Escape works while the title card is on screen.
[DefaultExecutionOrder(-400)]
public sealed class TideTimeline : MonoBehaviour
{
    const float DisplaySeconds = 2.2f;
    static TideTimeline current;
    static int entryHandle = -1, revealedFrame = -1;
    float elapsed;
    public static bool BlocksInput => current || revealedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void BeginRun()
    {
        entryHandle = revealedFrame = -1;
        current = null;
    }

    static string CardFor(string scene)
    {
        switch(scene)
        {
            case "05_MemoryCottage": return "ThreeYearsAgo";
            case "08_CottagePromise": return "FiveYearsAgo";
            case "11_ParkPromise": return "TenYearsAgo";
            case "12_StreetOrigin": return "FifteenYearsAgo";
            case "13_DataStation": return "TenMinutesLeft";
            default: return null;
        }
    }

    public static void ShowWhenEntering(TideHUD hud)
    {
        var scene = hud.root.gameObject.scene;
        string card = CardFor(scene.name);
        if (card == null || entryHandle == scene.handle) return;
        var art = Resources.Load<Texture2D>("Tide/Timeline/"+card);
        if (!art) { Debug.LogError("缺少时间卡 "+card); return; }

        var screen = new GameObject("剧情时间卡",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(Image));
        SceneManager.MoveGameObjectToScene(screen,scene);
        var canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        screen.GetComponent<Image>().color = Color.black;
        var image = new GameObject(card,typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));
        image.transform.SetParent(screen.transform,false);
        var picture=image.GetComponent<RawImage>();
        picture.texture=art;
        picture.raycastTarget=false;
        var fit=image.GetComponent<AspectRatioFitter>();
        fit.aspectRatio=(float)art.width/art.height;
        fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        entryHandle=scene.handle;
        current=screen.AddComponent<TideTimeline>();
    }

    void Update()
    {
        if(TidePause.MenuBlocksInput) return;
        elapsed+=Time.deltaTime;
        if(elapsed<DisplaySeconds) return;
        revealedFrame=Time.frameCount;
        current=null;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
