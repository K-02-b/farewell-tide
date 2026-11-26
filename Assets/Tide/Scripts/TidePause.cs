using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class TidePause : MonoBehaviour
{
    static TidePause current;
    static AudioSource backgroundMusic;
    static int consumedFrame=-1;
    internal static bool JournalOpen => current && current.journal != null && current.journal.Open;
    internal static bool MenuBlocksInput => (current && current.open) || JournalOpen || consumedFrame==Time.frameCount;
    public static bool BlocksInput => MenuBlocksInput || TideTimeline.BlocksInput;
    TideHUD hud;
    RectTransform shade, art, page;
    TideGuideBook guideBook;
    TideJournal journal;
    GameObject previousSelection;
    bool open, previousAudioPause;
    float previousSpeed;
    Slider volume;
    readonly List<Selectable> blockedControls=new List<Selectable>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        consumedFrame=-1;
        AudioListener.volume=PlayerPrefs.GetFloat("Tide.Volume",1);
    }
    public static void EnsureForScene()
    {
        var scene=SceneManager.GetActiveScene();
        if(current && current.gameObject.scene==scene)return;
        current=new GameObject("暂停菜单").AddComponent<TidePause>();
        TideCampaignProgress.Arrive(scene.name);
        UpdateMusic(scene);
    }

    static void UpdateMusic(Scene scene)
    {
        var path=scene.name.StartsWith("Trial",System.StringComparison.Ordinal) ? "Tide/Sound/TrialBgm" : "Tide/Sound/EndingMeadow";
        var clip=Resources.Load<AudioClip>(path);
        if(!clip)
        {
            if(backgroundMusic)
            {
                backgroundMusic.Stop();
                Destroy(backgroundMusic.gameObject);
                backgroundMusic=null;
            }
            return;
        }

        if(!backgroundMusic)
        {
            var player=new GameObject("背景配乐");
            DontDestroyOnLoad(player);
            backgroundMusic=player.AddComponent<AudioSource>();
            backgroundMusic.playOnAwake=false;
            backgroundMusic.loop=true;
            backgroundMusic.volume=.65f;
        }
        if(backgroundMusic.clip!=clip)
        {
            backgroundMusic.clip=clip;
            backgroundMusic.Play();
        }
    }

    void Update()
    {
        if(JournalOpen){journal.HandleKeys();return;}
        if(consumedFrame==Time.frameCount)return;
        if(!open && !Input.GetKeyDown(KeyCode.Escape) && Input.GetKeyDown(KeyCode.Tab))
        {
            RequestJournal();
            return;
        }
        if(!Input.GetKeyDown(KeyCode.Escape))return;
        consumedFrame=Time.frameCount;
        if(!open)Open();else if(guideBook!=null)CloseGuideBook();else if(page && page.gameObject.activeSelf)Back();else Close();
    }
    internal static void RequestJournal()
    {
        if(!current || BlocksInput || !TideJournal.HasAny)return;
        var trial=FindObjectOfType<TideTrial>();
        if(trial)trial.RequestJournalList();
        else ShowJournal(null);
    }
    internal static bool ShowJournal(TextAsset document)
    {
        if(!current || BlocksInput || !TideJournal.HasAny)return false;
        if(current.journal==null)current.journal=new TideJournal(()=>consumedFrame=Time.frameCount);
        current.journal.Show(document);
        return true;
    }
    void Open()
    {
        open=true;previousSpeed=Time.timeScale;previousAudioPause=AudioListener.pause;
        Time.timeScale=0;AudioListener.pause=true;
        previousSelection=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;
        foreach(var control in Selectable.allSelectablesArray) {
            if(!control.interactable || (shade && control.transform.IsChildOf(shade)))continue;
            control.interactable=false;blockedControls.Add(control);
        }
        if(!shade)Build();
        shade.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        volume.SetValueWithoutNotify(AudioListener.volume);
    }
    void Restore()
    {
        open=false;Time.timeScale=previousSpeed;AudioListener.pause=previousAudioPause;
        foreach(var control in blockedControls)if(control)control.interactable=true;
        blockedControls.Clear();
    }
    void Close()
    {
        consumedFrame=Time.frameCount;Restore();shade.gameObject.SetActive(false);
        if(EventSystem.current)EventSystem.current.SetSelectedGameObject(previousSelection);
    }
    void OnDestroy(){if(journal!=null && journal.Open)journal.Close();if(open)Restore();}
    void Build()
    {
        hud=new TideHUD();hud.root.GetComponent<Canvas>().sortingOrder=300;
        shade=hud.Panel("遮罩",.5f,.5f,0,0,0,0,new Color(0,0,0,.58f));
        shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
        var blocker=shade.GetComponent<Image>();blocker.sprite=null;blocker.raycastTarget=true;
        art = hud.Box("暂停", shade, Vector2.one * .5f, Vector2.zero,
            new Vector2(512, 384), Color.white, TideArt.Pic("Ending/Pause"));
        Hotspot("退出游戏", 155, 145, 166, 52, QuitGame);
        Hotspot("返回主页", -118, -25, 220, 72, () => Load("00_Title"));
        Hotspot("重新开始", 119, -25, 220, 72, () => Restart(SceneManager.GetActiveScene().name));
        Hotspot("关卡选择", -118, -111, 220, 72, () => Subpage(true));
        Hotspot("操作说明", 119, -111, 220, 72, () => Subpage(false));
        BuildVolumeSlider();
    }

    void BuildVolumeSlider()
    {
        var area = hud.Box("音量", art, Vector2.one * .5f, new Vector2(59, 65),
            new Vector2(322, 46), Color.clear);
        area.GetComponent<Image>().raycastTarget = true;
        volume = area.gameObject.AddComponent<Slider>();

        var track = hud.Box("滑轨", area, Vector2.one * .5f, Vector2.zero,
            new Vector2(308, 6), new Color32(174, 163, 143, 255));
        track.GetComponent<Image>().sprite = null;
        var fill = hud.Box("音量值", track, Vector2.zero, Vector2.zero, Vector2.zero,
            new Color32(162, 100, 37, 255));
        fill.GetComponent<Image>().sprite = null;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        var handle = hud.Box("滑块", track, Vector2.one * .5f, Vector2.zero,
            new Vector2(14, 14), new Color32(58, 52, 45, 255));
        handle.GetComponent<Image>().sprite = null;
        var face = hud.Box("滑块中心", handle, Vector2.one * .5f, Vector2.zero,
            new Vector2(8, 12), new Color32(244, 195, 112, 255));
        face.GetComponent<Image>().sprite = null;
        volume.fillRect = fill;
        volume.handleRect = handle;
        volume.targetGraphic = handle.GetComponent<Image>();
        volume.onValueChanged.AddListener(v => {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("Tide.Volume", v);
        });
    }
    void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void Hotspot(string label,float x,float y,float width,float height,UnityEngine.Events.UnityAction click)
    {
        var r=hud.Box(label,art,Vector2.one*.5f,new Vector2(x,y),new Vector2(width,height),Color.clear);
        hud.Click(r,()=>click());
    }
    void Load(string name){Close();SceneManager.LoadScene(name);}
    void Restart(string name){TideTrial.ClearSavedLanding(name);Load(name);}
    void Back()
    {
        page.gameObject.SetActive(false);Destroy(page.gameObject);page=null;art.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }
    void OpenGuideBook()
    {
        if(guideBook!=null)return;
        page.gameObject.SetActive(false);
        string last=PlayerPrefs.GetString(TideCampaignProgress.LastTrialKey);
        int room=1;
        if(last.StartsWith("Trial") && last.Length>=7 && int.TryParse(last.Substring(5,2),out int saved))room=saved;
        guideBook=new TideGuideBook(hud,shade,room,CloseGuideBook);
        EventSystem.current.SetSelectedGameObject(null);
    }
    void CloseGuideBook()
    {
        guideBook.panel.gameObject.SetActive(false);
        Destroy(guideBook.panel.gameObject);guideBook=null;
        page.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }
    void Subpage(bool levels)
    {
        art.gameObject.SetActive(false);
        page=hud.Box(levels?"关卡选择":"操作说明",shade,Vector2.one*.5f,Vector2.zero,levels?new Vector2(620,456):new Vector2(520,394),TideHUD.ink);
        hud.Caption(hud.Box("标题",page,new Vector2(.5f,1),new Vector2(0,-16),new Vector2(460,44),Color.clear),page.name,25,TextAnchor.MiddleCenter);
        if(levels){
            string currentScene=SceneManager.GetActiveScene().name;
            string lastScene=TideCampaignProgress.ResumeScene;
            for(int i=0;i<TideCampaignProgress.Count;i++){
                var entry=TideCampaignProgress.At(i);
                bool unlocked=TideCampaignProgress.IsUnlocked(i);
                bool here=entry.Contains(currentScene),last=TideCampaignProgress.HasSavedRun&&entry.Contains(lastScene);
                string status=!unlocked?"　未解锁":here?"　当前":last&&currentScene=="00_Title"?"　上次":"";
                Color fill=!unlocked?new Color32(48,45,42,255):here||last?new Color32(104,79,46,255):new Color32(66,59,48,255);
                int column=i==TideCampaignProgress.Count-1?0:i%3-1;
                var b=hud.Box(entry.StartScene,page,new Vector2(.5f,1),new Vector2(column*192,-68-i/3*52),new Vector2(180,42),fill);
                var text=hud.Caption(b,entry.label+status,18,TextAnchor.MiddleCenter);
                if(unlocked)hud.Click(b,()=>Restart(entry.StartScene));
                else text.color=new Color32(186,179,166,255);
            }
        }else{
            var body=hud.Box("按键",page,Vector2.one*.5f,new Vector2(0,8),new Vector2(460,236),Color.clear);
            hud.Caption(body,"A D 或左右方向键　移动\n空格、W 或上方向键　跳跃\nShift　切换操作视角，拖动平台\n绿台按跳跃键高跳，紫台按 V 冲刺\nE 或回车互动，空格对话，长按 2 秒快进\nR　重试跳跃场景　　 Esc　暂停或继续",18,TextAnchor.MiddleLeft);
        }
        if(levels){
            var guides=hud.Box("关卡提示",page,new Vector2(.5f,0),new Vector2(96,18),new Vector2(180,42),new Color(.34f,.27f,.19f));
            hud.Caption(guides,"关卡提示",19,TextAnchor.MiddleCenter);hud.Click(guides,OpenGuideBook);
        }
        var back=hud.Box("返回",page,new Vector2(.5f,0),new Vector2(levels?-96:0,18),new Vector2(levels?180:124,42),new Color(.26f,.23f,.19f));
        hud.Caption(back,"返回",19,TextAnchor.MiddleCenter);hud.Click(back,Back);
    }
}
