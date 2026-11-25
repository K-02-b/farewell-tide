using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TideStreet : MonoBehaviour
{
    public enum Cue { None, ExpandPanel, Permission, ElderEnters, HusbandApproaches }
    [System.Serializable]
    public struct Line
    {
        public string speaker;
        [TextArea] public string text;
        public Cue cue;
        public Line(string speaker, string text, Cue cue = Cue.None)
        {
            this.speaker = speaker;
            this.text = text;
            this.cue = cue;
        }
    }

    public SpriteRenderer wife, husband, elder, street, panel;
    public string nextScene = TideEnding.Station;
    public Line[] lines;
    Camera view;
    TideHUD hud;
    RectTransform dialogue, yes;
    Text nameLabel, words;
    Sprite[] wifeIdle, manIdle, manWalk, elderIdle, elderWalk;
    int index, lastInput = -1;
    float zoom;
    bool busy, elderWalking, manWalking, finished;

    void Start()
    {
        view = Camera.main;
        elder.sortingOrder=wife.sortingOrder-1;
        wifeIdle = TideArt.Frames("Wife");
        manIdle = TideArt.Frames("ManSide");
        manWalk = TideArt.Frames("ManWalk");
        elderIdle = TideArt.Frames("Elder");
        elderWalk = TideArt.Frames("ElderWalk");
        if (!street.sprite) street.sprite = TideArt.Pic("Art/StreetShops");
        street.color = Color.white;
        TideArt.Fit(street, street.sprite, 9);

        hud = new TideHUD(true);
        dialogue = hud.Panel("对话",.5f,.5f,0,0,900,112,TideHUD.ink);
        nameLabel = hud.Caption(hud.Box("名字",dialogue,Vector2.up,new Vector2(20,-7),
            new Vector2(840,30),Color.clear),"",20,TextAnchor.MiddleLeft);
        nameLabel.color = new Color(.92f,.77f,.46f);
        words = hud.Caption(hud.Box("台词",dialogue,Vector2.zero,new Vector2(20,9),
            new Vector2(860,65),Color.clear),"",24,TextAnchor.MiddleLeft);
        hud.Click(dialogue, ContinueDialogue);
        yes = hud.Box("确认权限",dialogue,new Vector2(1,.5f),new Vector2(-28,-10),
            new Vector2(82,40),TideHUD.ink);
        hud.Caption(yes,"是",22,TextAnchor.MiddleCenter);
        hud.Click(yes, Advance);
        ShowLine();
    }

    void ShowLine()
    {
        busy = false;
        var line = lines[index];
        nameLabel.text = line.speaker;
        words.text = line.text;
        yes.gameObject.SetActive(line.cue == Cue.Permission);
        dialogue.gameObject.SetActive(true);
    }

    void ContinueDialogue()
    {
        if (!busy && !finished && lines[index].cue != Cue.Permission) Advance();
    }

    void Advance()
    {
        if (busy || finished || TidePause.BlocksInput || lastInput == Time.frameCount) return;
        lastInput = Time.frameCount;
        dialogue.gameObject.SetActive(false);
        if (++index == lines.Length)
        {
            finished = true;
            SceneManager.LoadScene(nextScene);
            return;
        }
        switch (lines[index].cue)
        {
            case Cue.ExpandPanel: StartCoroutine(RevealPanel()); break;
            case Cue.ElderEnters: StartCoroutine(BringElderIn()); break;
            case Cue.HusbandApproaches: StartCoroutine(BringHusbandCloser()); break;
            default: ShowLine(); break;
        }
    }

    IEnumerator RevealPanel()
    {
        busy = true;
        panel.enabled = true;
        for (float t = 0; t < 1; t += Time.deltaTime / .65f)
        {
            zoom = Mathf.SmoothStep(0,1,t);
            yield return null;
        }
        zoom = 1;
        ShowLine();
    }

    IEnumerator BringElderIn()
    {
        busy = elderWalking = true;
        elder.gameObject.SetActive(true);
        wife.flipX = false;
        var target = new Vector3(wife.transform.position.x - 1.55f, elder.transform.position.y,0);
        while (elder.transform.position != target)
        {
            elder.transform.position = Vector3.MoveTowards(elder.transform.position,target,3.2f*Time.deltaTime);
            yield return null;
        }
        elderWalking = false;
        ShowLine();
    }

    IEnumerator BringHusbandCloser()
    {
        busy = manWalking = true;
        yield return TideLook.WalkToX(husband.transform,wife.transform.position.x+1.15f,3.2f);
        manWalking = false;
        ShowLine();
    }

    void Update()
    {
        if(TidePause.BlocksInput)return;
        Animate(wife,wifeIdle,6);
        var husbandFrames=manWalking?manWalk:manIdle;
        TideLook.PoseHusband(husband,husbandFrames[(int)(Time.time*(manWalking?9:6))%husbandFrames.Length],wife,2.12f,!manWalking);
        if (elder.gameObject.activeSelf) Animate(elder,elderWalking ? elderWalk : elderIdle, elderWalking ? 10 : 6);
        if (!busy && !finished && (Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Return))) Advance();
        else if (Input.GetKeyDown(KeyCode.Space)) ContinueDialogue();
    }

    static void Animate(SpriteRenderer actor, Sprite[] frames, int rate)
    {
        TideArt.Fit(actor,frames[(int)(Time.time*rate)%frames.Length],2.12f);
    }

    void LateUpdate()
    {
        Bounds bounds = panel.bounds;
        Vector3 center = Vector3.Lerp(Vector3.zero,bounds.center,zoom);
        TideArt.CameraFrame(view,Mathf.Lerp(16,bounds.size.x,zoom),Mathf.Lerp(9,bounds.size.y,zoom),center.x,center.y);
        if (!dialogue.gameObject.activeSelf) return;
        // Place dialogue inside the lower-left console bay after the camera pulls back.
        Vector2 min = zoom < 1 ? new Vector2(-6.9f,2.45f) : new Vector2(bounds.min.x+.015f*bounds.size.x,bounds.min.y+.025f*bounds.size.y);
        Vector2 max = zoom < 1 ? new Vector2(6.9f,4.2f) : new Vector2(bounds.min.x+.71f*bounds.size.x,bounds.min.y+.20f*bounds.size.y);
        Vector2 a = CanvasPoint(min), b = CanvasPoint(max);
        dialogue.anchoredPosition = (a+b)*.5f;
        dialogue.localScale = Vector3.one*Mathf.Min((b.x-a.x)/900,(b.y-a.y)/112);
    }

    Vector2 CanvasPoint(Vector2 world)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(hud.root,view.WorldToScreenPoint(world),null,out var point);
        return point;
    }
}
