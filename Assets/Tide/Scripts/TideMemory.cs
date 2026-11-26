using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TideMemory : MonoBehaviour
{
    public enum Setting { Cottage, Shore, Tower, Promise, Letter, Reunion, Park }
    enum Phase { Dialogue, Arrival, Explore, Flower, Departing, Reading, Crying, WalkingAway, WarningGate }

    public Setting setting;
    public SpriteRenderer player, wife, rememberedJosie, gate;
    public SpriteRenderer seated, parkFlowers;
    Sprite[] seatedFrames;
    public SpriteRenderer embrace;
    public SpriteRenderer book;
    public Sprite letterPaper;
    [TextArea(3,12)] public string letterText;
    Sprite[] wifeWalk, wifeCry;
    RectTransform paper;
    float cryStart;
    bool hasReadLetter;
    Sprite[] manSide, embraceFrames;
    float embraceStart;
    public float exitX;
    public string nextScene;
    [TextArea] public string[] lines;
    public string[] speakers;

    Phase phase;
    int line, lastDialogueInput=-1;
    readonly TideDialogueSkip dialogueSkip = new TideDialogueSkip();
    float arrivalDelay, nextStep;
    Sprite[] idle, walk, side, front;
    TideHUD ui;
    RectTransform dialogue, prompt, flower;
    Text speaker, words;
    Image portrait;
    Sprite manPortrait, wifePortrait;
    AudioSource sound;
    AudioClip footstep;
    Camera sceneCamera;

    void Start()
    {
        sceneCamera = Camera.main;
        TideArt.CameraFrame(sceneCamera, 16, 9);
        idle = TideArt.Frames("ManIdle");
        walk = TideArt.Frames("ManWalk");
        side = TideArt.Frames("Wife");
        front = TideArt.Frames("WifeFront");
        if (setting == Setting.Cottage || setting == Setting.Shore || setting == Setting.Tower || setting == Setting.Letter)
        {
            float height = wife.bounds.size.y, floor = wife.bounds.min.y;
            TideLook.FitHusbandAtFeet(player, idle[0], height, floor);
            if (rememberedJosie) TideLook.FitHusbandAtFeet(rememberedJosie, idle[0], height, floor);
        }
        if (setting == Setting.Promise || setting == Setting.Reunion)
        {
            manSide = TideArt.Frames("ManSide");
            embraceFrames = TideArt.Frames("Embrace");
        }
        if (setting == Setting.Letter || setting == Setting.Reunion)
        {
            wifeWalk = TideArt.Frames("WifeWalk");
            wifeCry = TideArt.Frames("WifeCry");
            if (setting == Setting.Letter) player.gameObject.SetActive(false);
        }
        if (setting == Setting.Park) seatedFrames = TideArt.Frames("Seated");
        manPortrait = Face(idle[0]);
        wifePortrait = Face(front[0]);
        sound = gameObject.AddComponent<AudioSource>();
        footstep = Resources.Load<AudioClip>("Tide/Sound/" + (setting == Setting.Cottage || setting == Setting.Letter ? "Wood" : "Soil"));

        ui = new TideHUD(true);
        dialogue = ui.Panel("对话", .5f, 1, 0, -24, 900, 156, TideHUD.ink);
        ui.Border(dialogue, new Color(.73f, .60f, .38f, .65f));
        ui.DialogueContinue(dialogue);
        portrait = ui.Box("头像", dialogue, new Vector2(0,.5f), new Vector2(30,0),
            new Vector2(80,88), Color.white, wifePortrait).GetComponent<Image>();
        speaker = ui.Caption(ui.Box("名字", dialogue, Vector2.up, new Vector2(134,-16),
            new Vector2(630,28), Color.clear), "", 19, TextAnchor.MiddleLeft);
        speaker.color = new Color(.92f,.77f,.46f);
        words = ui.Caption(ui.Box("台词", dialogue, Vector2.zero, new Vector2(134,36),
            new Vector2(728,76), Color.clear), "", 23, TextAnchor.MiddleLeft);
        words.resizeTextForBestFit = true;
        words.resizeTextMinSize = 17;
        words.resizeTextMaxSize = 23;
        words.verticalOverflow = VerticalWrapMode.Truncate;
        ui.Click(dialogue, Advance);
        prompt = ui.Panel("交互提示", .5f, .5f, 0, 0, setting == Setting.Shore ? 214 : 132, 34, TideHUD.ink);
        string action = setting == Setting.Letter ? "[E] 读信" : setting == Setting.Shore ? "[E] 前往灯塔顶端" : setting == Setting.Cottage ? "[E] 出门" : "[E] 进入";
        ui.Caption(prompt, action, 18, TextAnchor.MiddleCenter);
        prompt.gameObject.SetActive(false);
        if (gate) gate.gameObject.SetActive(false);
        ShowLine();
    }

    static Sprite Face(Sprite sprite)
    {
        Rect rect = sprite.rect;
        rect.y += rect.height * .57f;
        rect.height *= .43f;
        return Sprite.Create(sprite.texture, rect, new Vector2(.5f,.5f), sprite.pixelsPerUnit);
    }

    void OnDestroy()
    {
        if (manPortrait) Destroy(manPortrait);
        if (wifePortrait) Destroy(wifePortrait);
    }

    void ShowLine()
    {
        if ((setting == Setting.Promise && line == 7) || (setting == Setting.Reunion && line == 2))
        {
            player.gameObject.SetActive(false);
            wife.gameObject.SetActive(false);
            embraceStart = Time.time;
            embrace.gameObject.SetActive(true);
        }
        if (setting == Setting.Reunion && line == 3)
        {
            embrace.gameObject.SetActive(false);
            player.gameObject.SetActive(true);
            wife.gameObject.SetActive(true);
        }
        lastDialogueInput=Time.frameCount;
        phase = Phase.Dialogue;
        dialogue.gameObject.SetActive(true);
        speaker.text = speakers[line];
        words.text = lines[line];
        portrait.sprite = speakers[line] == "乔西" ? manPortrait : wifePortrait;
    }

    void Advance()
    {
        if (phase != Phase.Dialogue || lastDialogueInput==Time.frameCount || TidePause.BlocksInput) return;
        lastDialogueInput=Time.frameCount;
        dialogue.gameObject.SetActive(false);
        line++;
        if (setting == Setting.Park)
        {
            if (line < lines.Length) ShowLine();
            else
            {
                phase = Phase.Departing;
                if (parkFlowers) StartCoroutine(TideLook.FadeOut(parkFlowers));
                StartCoroutine(TideLook.FadeOut(seated, ShowWarningGate));
            }
            return;
        }
        if (setting == Setting.Reunion)
        {
            if (line == 1) phase = Phase.Arrival;
            else if (line == 6) phase = Phase.WalkingAway;
            else if (line < lines.Length) ShowLine();
            else
            {
                phase = Phase.Departing;
                StartCoroutine(TideLook.FadeOut(wife, ShowWarningGate));
            }
            return;
        }
        if (setting == Setting.Letter)
        {
            if (line == 1) phase = Phase.Arrival;
            else if (line == 2)
            {
                player.gameObject.SetActive(true);
                phase = Phase.Explore;
            }
            else
            {
                phase = Phase.Departing;
                StartCoroutine(TideLook.FadeOut(wife, () => SceneManager.LoadScene(nextScene)));
            }
            return;
        }
        if (setting == Setting.Cottage && line == 1)
        {
            phase = Phase.Arrival;
            arrivalDelay = .45f;
            return;
        }
        if (line < lines.Length) { ShowLine(); return; }
        if (setting == Setting.Promise)
        {
            phase = Phase.Departing;
            StartCoroutine(TideLook.FadeOut(embrace, () => SceneManager.LoadScene(nextScene)));
            return;
        }
        StartCoroutine(TideLook.FadeOut(wife));
        if (setting == Setting.Tower)
        {
            phase = Phase.Flower;
            flower = ui.Panel("鱼钩展示", .5f, .5f, 0, 0, 0, 0, Color.clear);
            var hook = ui.Box("鱼钩", flower, Vector2.one * .5f, Vector2.zero,
                Vector2.zero, Color.white, TideArt.Pic("Art/TideFlower"));
            hook.anchorMin = new Vector2(.77f, .52f);
            hook.anchorMax = new Vector2(.92f, .80f);
            hook.offsetMin = hook.offsetMax = Vector2.zero;
            var image = hook.GetComponent<Image>();
            image.canvasRenderer.SetAlpha(0);
            image.CrossFadeAlpha(1, .24f, true);
            FitHookOverlay();
            ui.Click(flower, CloseFlower);
            return;
        }
        StartCoroutine(TideLook.FadeOut(rememberedJosie));
        phase = Phase.Explore;
    }

    void ShowWarningGate()
    {
        gate.gameObject.SetActive(true);
        phase = Phase.WarningGate;
    }

    void FitHookOverlay()
    {
        var viewport = sceneCamera.rect;
        flower.anchorMin = viewport.min;
        flower.anchorMax = viewport.max;
        flower.offsetMin = flower.offsetMax = Vector2.zero;
    }

    void CloseFlower()
    {
        if (phase != Phase.Flower) return;
        flower.gameObject.SetActive(false);
        gate.gameObject.SetActive(true);
        phase = Phase.Explore;
    }

    static void Animate(SpriteRenderer actor, Sprite[] frames, float rate)
    {
        actor.sprite = frames[(int)(Time.time * rate) % frames.Length];
    }

    void Update()
    {
        if(TidePause.BlocksInput){dialogueSkip.Reset();return;}
        TideArt.CameraFrame(sceneCamera, 16, 9);
        bool betweenLines = phase == Phase.Arrival || phase == Phase.WalkingAway || phase == Phase.Crying;
        bool repeat = dialogueSkip.ShouldRepeat(phase == Phase.Dialogue, betweenLines);
        bool interact = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)
            || ((phase == Phase.Dialogue || phase == Phase.Reading || phase == Phase.Flower) && Input.GetKeyDown(KeyCode.Space))
            || repeat;
        if (phase == Phase.WarningGate)
        {
            var tip = gate.bounds.center + Vector3.up * (gate.bounds.extents.y + .38f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root, sceneCamera.WorldToScreenPoint(tip), null, out var point);
            prompt.anchoredPosition = point;
            prompt.gameObject.SetActive(true);
            bool clicked = Input.GetMouseButtonDown(0) &&
                gate.bounds.Contains((Vector2)sceneCamera.ScreenToWorldPoint(Input.mousePosition));
            if (interact || clicked) SceneManager.LoadScene(nextScene);
            return;
        }
        if (setting == Setting.Park)
        {
            Animate(seated, seatedFrames, 6);
            if (interact && phase == Phase.Dialogue) Advance();
            return;
        }
        if (setting == Setting.Reunion)
        {
            UpdateReunion(interact);
            return;
        }
        if (setting == Setting.Letter)
        {
            UpdateLetter(interact);
            return;
        }
        if (setting == Setting.Promise)
        {
            if (embrace.gameObject.activeSelf)
                embrace.sprite = embraceFrames[(int)((Time.time - embraceStart) * 6) % embraceFrames.Length];
            else
            {
                Animate(wife, side, 6);
                TideLook.PoseHusband(player,manSide[(int)(Time.time*6)%manSide.Length],wife,2.65f,true);
            }
            if (interact && phase == Phase.Dialogue) Advance();
            return;
        }
        if (wife.gameObject.activeSelf)
        {
            bool profile = setting == Setting.Cottage || (setting == Setting.Shore && line > 0);
            var frames = profile ? side : front;
            TideArt.Fit(wife, frames[(int)(Time.time * 6) % frames.Length], setting == Setting.Cottage ? 2.7f : 2.2f);
            wife.flipX = profile;
        }
        if (rememberedJosie && rememberedJosie.gameObject.activeSelf)
        {
            if (setting == Setting.Cottage)
                rememberedJosie.enabled = rememberedJosie.transform.position.x <= TideLook.CottageWalkRight;
            Animate(rememberedJosie, phase == Phase.Arrival ? walk : idle, phase == Phase.Arrival ? 9 : 6);
        }
        if (phase == Phase.Arrival)
        {
            arrivalDelay -= Time.deltaTime;
            if (arrivalDelay > 0) return;
            var actor = rememberedJosie.transform;
            var destination = new Vector3(wife.transform.position.x + 1.55f, actor.position.y, 0);
            actor.position = Vector3.MoveTowards(actor.position, destination, 3.6f * Time.deltaTime);
            if (actor.position == destination) ShowLine();
            return;
        }
        if (phase == Phase.Dialogue) { Animate(player, idle, 6); if (interact) Advance(); return; }
        if (phase == Phase.Flower) { FitHookOverlay(); if (interact) CloseFlower(); return; }

        Vector3 pos = MovePlayer();
        bool near = Mathf.Abs(pos.x - exitX) < 1.35f;
        prompt.gameObject.SetActive(near);
        if (near)
        {
            var point = sceneCamera.WorldToScreenPoint(new Vector3(Mathf.Clamp(exitX,-6f,6f), pos.y + 1.55f, 0));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root, point, null, out var local);
            prompt.anchoredPosition = local;
        }
        bool clickedGate = gate && Input.GetMouseButtonDown(0) &&
            gate.bounds.Contains((Vector2)sceneCamera.ScreenToWorldPoint(Input.mousePosition));
        if ((near && interact) || clickedGate) SceneManager.LoadScene(nextScene);
    }
    Vector3 MovePlayer()
    {
        float dx = Input.GetAxisRaw("Horizontal");
        Vector3 pos = player.transform.position;
        bool cottage = setting == Setting.Cottage || setting == Setting.Letter;
        pos.x = Mathf.Clamp(pos.x + dx * 4 * Time.deltaTime,
            cottage ? TideLook.CottageWalkLeft : setting == Setting.Tower ? -4.3f : -7.4f,
            cottage ? TideLook.CottageWalkRight : setting == Setting.Tower ? 4.4f : 7.4f);
        player.transform.position = pos;
        Animate(player, dx == 0 ? idle : walk, dx == 0 ? 6 : 9);
        if (dx != 0)
        {
            player.flipX = dx > 0;
            if (Time.time >= nextStep) { sound.PlayOneShot(footstep,(setting==Setting.Cottage||setting==Setting.Letter)?TideLook.WoodStepVolume:.28f); nextStep = Time.time + .38f; }
        }
        return pos;
    }

    void UpdateReunion(bool interact)
    {
        if (embrace.gameObject.activeSelf)
        {
            embrace.sprite = embraceFrames[(int)((Time.time - embraceStart) * 6) % embraceFrames.Length];
        }
        else
        {
            bool approaching = phase == Phase.Arrival;
            bool leaving = phase == Phase.WalkingAway;
            bool crying = !player.gameObject.activeSelf;
            wife.flipX = !crying;
            var wifeFrames = crying ? wifeCry : approaching ? wifeWalk : side;
            float height = 2.18f;
            TideArt.Fit(wife, wifeFrames[(int)(Time.time * (approaching ? 9 : 6)) % wifeFrames.Length], height);
            if (!crying)
            {
                var manFrames = leaving ? walk : manSide;
                TideLook.PoseHusband(player,manFrames[(int)(Time.time*(leaving?9:6))%manFrames.Length],wife,height,!leaving);
            }
            if (approaching || leaving)
            {
                Transform actor = approaching ? wife.transform : player.transform;
                float targetX = approaching ? player.transform.position.x - 1.3f : -9.5f;
                var mark = new Vector3(targetX, actor.position.y, 0);
                actor.position = Vector3.MoveTowards(actor.position, mark, (approaching ? 3.4f : 4f) * Time.deltaTime);
                if (actor.position == mark)
                {
                    if (leaving)
                    {
                        player.gameObject.SetActive(false);
                        wife.flipX = false;
                        TideArt.Fit(wife, wifeCry[0], height);
                    }
                    ShowLine();
                }
                return;
            }
        }
        if (phase == Phase.Dialogue && interact) Advance();
    }

    void UpdateLetter(bool interact)
    {
        if (hasReadLetter)
        {
            wife.sprite = wifeCry[(int)((Time.time - cryStart) * 6) % wifeCry.Length];
            if (phase == Phase.Crying && Time.time - cryStart >= .3f) ShowLine();
            else if (phase == Phase.Dialogue && interact) Advance();
            return;
        }
        if (phase == Phase.Arrival)
        {
            wife.flipX = true;
            TideArt.Fit(wife, wifeWalk[(int)(Time.time * 9) % wifeWalk.Length], 2.65f);
            var mark = new Vector3(-1.5f, wife.transform.position.y, 0);
            wife.transform.position = Vector3.MoveTowards(wife.transform.position, mark, 2.8f * Time.deltaTime);
            if (wife.transform.position == mark) ShowLine();
            return;
        }
        wife.flipX = line > 0;
        var still = line == 0 ? front : side;
        TideArt.Fit(wife, still[(int)(Time.time * 6) % still.Length], 2.65f);
        if (phase == Phase.Dialogue) { if (interact) Advance(); return; }
        if (phase == Phase.Reading && interact) { CloseLetter(); return; }
        if (phase != Phase.Explore) return;
        var pos = MovePlayer();
        bool near = Mathf.Abs(pos.x - book.transform.position.x) < 1.3f;
        prompt.gameObject.SetActive(near);
        if (!near) return;
        var point = sceneCamera.WorldToScreenPoint(book.bounds.center + Vector3.up * .85f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root, point, null, out var local);
        prompt.anchoredPosition = local;
        if (interact) OpenLetter();
    }

    void OpenLetter()
    {
        phase = Phase.Reading;
        prompt.gameObject.SetActive(false);
        sound.PlayOneShot(Resources.Load<AudioClip>("Tide/Sound/Paper"), .6f);
        Color tint = letterPaper ? Color.white : new Color(.96f,.91f,.78f);
        paper = ui.Panel("信纸", .5f, .5f, 0, 0, 560, 400, tint, letterPaper);
        if (letterPaper)
        {
            paper.anchorMin = new Vector2(.03f, .025f);
            paper.anchorMax = new Vector2(.97f, .975f);
            paper.offsetMin = paper.offsetMax = Vector2.zero;
            paper.GetComponent<Image>().preserveAspect = true;
        }
        else paper.GetComponent<Image>().sprite = null;
        for (int i = 0; !letterPaper && i < 8; i++)
        {
            var rule = ui.Box("信纸横线", paper, new Vector2(.5f,1), new Vector2(0,-70-i*36),
                new Vector2(464,1), new Color(.49f,.38f,.25f,.27f));
            rule.GetComponent<Image>().sprite = null;
        }
        if (!string.IsNullOrEmpty(letterText))
        {
            var body = ui.Box("信件正文", paper, new Vector2(.5f,.5f), Vector2.zero, new Vector2(490,332), Color.clear);
            ui.Caption(body, letterText, 23, TextAnchor.UpperLeft).color = new Color(.24f,.18f,.13f);
        }
        ui.Click(paper, CloseLetter);
    }

    void CloseLetter()
    {
        if (phase != Phase.Reading) return;
        paper.gameObject.SetActive(false);
        hasReadLetter = true;
        phase = Phase.Crying;
        cryStart = Time.time;
        wife.flipX = false;
        TideArt.Fit(wife, wifeCry[0], 2.65f);
    }

}
