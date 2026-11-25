using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TideEnding : MonoBehaviour
{
    public const string Station = "13_DataStation", Ruins = "14_CollapsingWorld", Meadow = "15_SunsetMeadow";
    public enum Location { Station, Ruins, Meadow }
    enum Phase { Acting, Dialogue, Choice, Exit, Crossing, End, BackupTerminal }
    public Location location;
    public SpriteRenderer hero, elder, wife, warning, vision, meadowBackground;
    public SpriteRenderer backupTerminal;
    public SpriteRenderer[] memories;
    [System.Serializable] public struct Line { public string speaker; [TextArea] public string text; }
    public Line[] conversation, beforeSunsetTurn, afterSunsetTurn;
    public Sprite manBack, wifeBack;
    public Sprite finalTitle;

    Phase phase;
    TideHUD hud;
    RectTransform dialogue, choice, prompt;
    Text speaker, words, promptText;
    Camera view;
    Sprite[] manIdle, manWalk, elderIdle, elderWalk, wifeSide, embrace;
    bool walking, elderMoving, wifeFacesAway, manFacesAway, lifting;
    float cameraRise;
    int index, nextMemory, meadowLine, lastFrame = -1;
    const float ActorHeight = 1.85f;
    const float RoomWalkSpeed = 3.8f;
    const float MemoryWalkSpeed = 2f;

    void Start()
    {
        view = Camera.main;
        hud = new TideHUD(true);
        if(location==Location.Station && hero && elder) hero.sortingOrder=elder.sortingOrder+1;
        if(location==Location.Meadow && hero && wife) hero.sortingOrder=wife.sortingOrder+1;
        manIdle = TideArt.Frames("ManSide");
        manWalk = TideArt.Frames("ManWalk");
        bool meadow=location==Location.Meadow;
        dialogue = hud.Panel("对话",.5f,meadow?0:1,0,meadow?12:-22,900,meadow?104:158,TideHUD.ink);
        hud.Border(dialogue,new Color(.73f,.6f,.38f,.65f));
        speaker = hud.Caption(hud.Box("名字",dialogue,Vector2.up,new Vector2(20,meadow?-5:-10),new Vector2(850,30),Color.clear),"",20,TextAnchor.MiddleLeft);
        speaker.color = new Color(.92f,.77f,.46f);
        words = hud.Caption(hud.Box("台词",dialogue,Vector2.zero,new Vector2(20,meadow?6:12),new Vector2(860,meadow?60:102),Color.clear),"",22,TextAnchor.MiddleLeft);
        words.resizeTextForBestFit=true; words.resizeTextMinSize=18; words.resizeTextMaxSize=22;
        hud.Click(dialogue,Advance);
        prompt=hud.Panel("交互提示",.5f,0,0,34,210,38,TideHUD.ink);
        promptText=hud.Caption(prompt,"",18,TextAnchor.MiddleCenter);
        prompt.gameObject.SetActive(false);
        if(location==Location.Station)
        {
            elderIdle=TideArt.Frames("Elder"); elderWalk=TideArt.Frames("ElderWalk");
            hud.Click(prompt,TryBackup);
            choice=hud.Panel("备份选择",.5f,.5f,0,0,410,148,TideHUD.ink);
            hud.Caption(hud.Box("问题",choice,Vector2.up,new Vector2(0,-12),new Vector2(410,54),Color.clear),"是否备份数据",26,TextAnchor.MiddleCenter);
            Option("是",-75,true); Option("否",75,false);
            choice.gameObject.SetActive(false);
            Say(conversation[0]);
        }
        else if(location==Location.Ruins)
        {
            embrace=TideArt.Frames("Embrace");
            dialogue.gameObject.SetActive(false); phase=Phase.Crossing;
        }
        else
        {
            wifeSide=TideArt.Frames("Wife"); wifeFacesAway=true;
            Say(new Line {speaker="乔西",text="华娜！"});
        }
    }

    void Option(string label,float x,bool answer)
    {
        var button=hud.Box(label,choice,new Vector2(.5f,0),new Vector2(x,20),new Vector2(100,46),TideHUD.ink);
        hud.Caption(button,label,23,TextAnchor.MiddleCenter);hud.Click(button,()=>Choose(answer));
    }

    void Say(Line line)
    {
        phase=Phase.Dialogue; speaker.text=line.speaker; words.text=line.text;dialogue.gameObject.SetActive(true);
    }

    void Choose(bool answer)
    {
        if(phase!=Phase.Choice || TidePause.BlocksInput)return;
        lastFrame=Time.frameCount; choice.gameObject.SetActive(false);
        if(answer)
        {
            phase=Phase.BackupTerminal;
            return;
        }
        index=1;
        Say(conversation[index]);
    }

    void Advance()
    {
        if(phase!=Phase.Dialogue || lastFrame==Time.frameCount || TidePause.BlocksInput)return;
        lastFrame=Time.frameCount;phase=Phase.Acting;dialogue.gameObject.SetActive(false);
        if(location==Location.Meadow){StartCoroutine(MeadowBeat());return;}
        if(index==0){choice.gameObject.SetActive(true);phase=Phase.Choice;return;}
        index++;
        if(index>=conversation.Length){hero.flipX=true;phase=Phase.Exit;return;}
        StartCoroutine(StationBeat());
    }

    IEnumerator StationBeat()
    {
        // The cue precedes the corresponding spoken line.
        switch(index)
        {
            case 3: case 5: yield return WalkTo(hero,hero.transform.position.x+1.0f);break;
            case 11: hero.flipX=false;yield return new WaitForSeconds(.26f);break;
            case 12: yield return Reveal(warning,.28f);break;
            case 14:
                yield return WalkTo(hero,hero.transform.position.x-.6f);
                hero.flipX=true; yield return Reveal(vision,.35f,.36f);break;
            case 16: yield return WalkTo(elder,elder.transform.position.x-.85f);break;
        }
        Say(conversation[index]);
    }

    IEnumerator WalkTo(SpriteRenderer actor,float x)
    {
        bool isHero=actor==hero;
        if(isHero)walking=true;else elderMoving=true;
        actor.flipX=x>actor.transform.position.x;
        yield return TideLook.WalkToX(actor.transform,x,2.8f);
        if(isHero)walking=false;else elderMoving=false;
    }

    IEnumerator Reveal(SpriteRenderer item,float duration,float opacity=1)
    {
        item.color=new Color(1,1,1,0);item.gameObject.SetActive(true);
        for(float t=0;t<duration;t+=Time.deltaTime) {
            item.color=new Color(1,1,1,opacity*Mathf.SmoothStep(0,1,t/duration));yield return null;
        }
        item.color=new Color(1,1,1,opacity);
    }

    void Update()
    {
        if(TidePause.BlocksInput)return;
        if(lifting) {
            // Reserve a little more sky for each page, even if the player lingers.
            int pagesRead=meadowLine-beforeSunsetTurn.Length;
            float share=phase==Phase.End ? 1f : (float)pagesRead/(afterSunsetTurn.Length+1);
            float limit=(meadowBackground.bounds.max.y-4.5f)*share;
            cameraRise=Mathf.MoveTowards(cameraRise,limit,.095f*Time.deltaTime);
        }
        TideArt.CameraFrame(view,16,9,0,cameraRise);
        if(location==Location.Meadow) {
            float scale=hud.root.rect.height*view.rect.height/720f;
            dialogue.anchorMin=dialogue.anchorMax=new Vector2(.5f,view.rect.yMin);
            dialogue.localScale=Vector3.one*scale;
            dialogue.anchoredPosition=new Vector2(0,12*scale);
        }
        bool interact=Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Return);
        if(phase==Phase.Dialogue && (interact||Input.GetKeyDown(KeyCode.Space)))Advance();
        else if(phase==Phase.Exit || phase==Phase.Crossing || phase==Phase.BackupTerminal)Explore(interact);
        if(!manFacesAway)
        {
            int walkFramesPerSecond = phase == Phase.Crossing ? 5 : 9;
            var frames=walking?manWalk:manIdle;
            int fps=walking?walkFramesPerSecond:6;
            if(location==Location.Meadow)
                TideLook.PoseHusband(hero,frames[(int)(Time.time*fps)%frames.Length],wife,ActorHeight,!walking);
            else Animate(hero,frames,fps);
        }
        if(elder)Animate(elder,elderMoving?elderWalk:elderIdle,elderMoving?10:6);
        if(wife) {
            if(wifeFacesAway)TideArt.Fit(wife,wifeBack,ActorHeight);else Animate(wife,wifeSide,6);
        }
        if(location==Location.Ruins && memories[3].gameObject.activeSelf)
            memories[3].sprite=embrace[(int)(Time.time*6)%embrace.Length];
    }

    static void Animate(SpriteRenderer actor,Sprite[] frames,int fps)
    {
        TideArt.Fit(actor,frames[(int)(Time.time*fps)%frames.Length],ActorHeight);
    }

    void MovePlayer(float direction, float elapsed)
    {
        walking=direction!=0;
        if(!walking)return;
        hero.flipX=direction>0;
        float speed = phase == Phase.Crossing ? MemoryWalkSpeed : RoomWalkSpeed;
        Vector3 pos=hero.transform.position;
        pos.x=Mathf.Clamp(pos.x + direction * speed * elapsed,-7.45f,7.45f);
        hero.transform.position=pos;
    }

    void Explore(bool interact)
    {
        MovePlayer(Input.GetAxisRaw("Horizontal"), Time.deltaTime);
        float x=hero.transform.position.x;
        if(phase==Phase.BackupTerminal)
        {
            bool near=NearTerminal();
            prompt.gameObject.SetActive(true);
            promptText.text=near?"[E] 备份数据":"前往备份终端";
            bool clicked=false;
            if(near&&Input.GetMouseButtonDown(0))
            {
                Vector3 mouse=view.ScreenToWorldPoint(Input.mousePosition);
                mouse.z=backupTerminal.bounds.center.z;
                clicked=backupTerminal.bounds.Contains(mouse);
            }
            if(interact||clicked)TryBackup();
            return;
        }
        if(phase==Phase.Crossing) {
            while(nextMemory<memories.Length && x<=memories[nextMemory].transform.position.x+.45f)
                StartCoroutine(Reveal(memories[nextMemory++],.42f));
        }
        bool ready=phase==Phase.Exit ? x>=6.45f : x<=-6.85f;
        prompt.gameObject.SetActive(ready);
        if(!ready)return;
        promptText.text="[E] 离开";
        if(!interact || lastFrame==Time.frameCount)return;
        walking=false;prompt.gameObject.SetActive(false);
        SceneManager.LoadScene(phase==Phase.Exit?Ruins:Meadow);
    }

    bool NearTerminal()=>backupTerminal && Mathf.Abs(hero.transform.position.x-backupTerminal.transform.position.x)<1.45f;
    void TryBackup()
    {
        if(phase!=Phase.BackupTerminal||lastFrame==Time.frameCount||TidePause.BlocksInput||!NearTerminal())return;
        walking=false;phase=Phase.Acting;prompt.gameObject.SetActive(false);
        StartCoroutine(PlayBackupAndReturn());
    }

    IEnumerator PlayBackupAndReturn()
    {
        var frames=TideArt.Frames("TerminalLoading");
        if(frames.Length==0)
        {
            Debug.LogError("Missing backup animation frames in Tide/Anim/TerminalLoading.");
            SceneManager.LoadScene("00_Title");
            yield break;
        }

        var overlay=hud.Panel("终端读条",.5f,.5f,0,0,0,0,Color.white,frames[0]);
        overlay.anchorMin=Vector2.zero;
        overlay.anchorMax=Vector2.one;
        overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        var image=overlay.GetComponent<Image>();
        for(int i=0;i<frames.Length;i++)
        {
            image.sprite=frames[i];
            // Keep the closing words on screen longer than each progress frame.
            yield return new WaitForSeconds(i==frames.Length-1 ? 1.8f : .7f);
        }
        TideTitle.MarkEndingComplete();
        SceneManager.LoadScene("00_Title");
    }

    IEnumerator MeadowBeat()
    {
        if(meadowLine==0) {
            wifeFacesAway=false;wife.flipX=true;yield return new WaitForSeconds(.35f);
        }
        if(meadowLine==1)yield return WalkTo(hero,wife.transform.position.x+1.45f);
        if(meadowLine<beforeSunsetTurn.Length){Say(beforeSunsetTurn[meadowLine++]);yield break;}
        if(!lifting) {
            yield return new WaitForSeconds(.75f);
            manFacesAway=true;hero.flipX=false;TideLook.PoseHusband(hero,manBack,wife,ActorHeight,false);
            yield return new WaitForSeconds(.5f);wife.flipX=false;wifeFacesAway=true;
            lifting=true;
        }
        int after=meadowLine-beforeSunsetTurn.Length;
        if(after<afterSunsetTurn.Length){meadowLine++;Say(afterSunsetTurn[after]);yield break;}
        phase=Phase.End;
        var title=hud.Panel("再见潮汐",.5f,.5f,0,0,700,475,Color.white,finalTitle);
        var image=title.GetComponent<Image>();
        const float titleFade=.9f;
        image.canvasRenderer.SetAlpha(0);
        image.CrossFadeAlpha(1,titleFade,false);
        yield return new WaitForSeconds(titleFade);

        TideTitle.MarkEndingComplete();
        var skip=hud.Panel("跳过",1,0,-24,24,120,46,TideHUD.ink);
        hud.Caption(skip,"跳过",22,TextAnchor.MiddleCenter);
        hud.Click(skip,()=>
        {
            if(!TidePause.BlocksInput)SceneManager.LoadScene("00_Title");
        });
    }
}
