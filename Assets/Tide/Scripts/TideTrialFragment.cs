using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Collected journals survive an ordinary retry; entering the scene starts a fresh run.
public sealed class TideTrialFragment
{
    readonly TideHUD hud;
    readonly SpriteRenderer pickup;
    readonly string heading, text;
    readonly List<string> pages=new List<string>();
    RectTransform reader;
    Text body, pageCount, nextLabel;
    Button previous;
    int currentPage, consumedFrame=-1;

    public bool Reading => reader && reader.gameObject.activeSelf;

    public TideTrialFragment(TideHUD hud,Transform owner,Vector2 position,TextAsset document)
    {
        this.hud=hud;
        string source=document.text.Replace("\r\n","\n");
        int start=source.IndexOf('\n');
        heading=start<0 ? document.name : source.Substring(0,start);
        text=start<0 ? source : source.Substring(start+1);
        pickup=TideArt.Place(heading,TideArt.Pic("Art/LetterBook"),position.x,position.y,.64f,7);
        pickup.transform.SetParent(owner,true);
    }

    public bool Touch(Vector2 feet,float halfWidth,float height)
    {
        if(!pickup.enabled)return false;
        var character=new Bounds(feet+Vector2.up*(height*.5f),new Vector3(2*halfWidth,height,.1f));
        if(!character.Intersects(pickup.bounds))return false;
        pickup.enabled=false;
        consumedFrame=Time.frameCount;
        reader=hud.Panel("日志阅读",.5f,.5f,0,0,0,0,new Color(0,0,0,.65f));
        reader.anchorMin=Vector2.zero;reader.anchorMax=Vector2.one;
        reader.offsetMin=reader.offsetMax=Vector2.zero;
        var dim=reader.GetComponent<Image>();dim.sprite=null;dim.raycastTarget=true;
        var panel=hud.Box("日志纸面",reader,Vector2.one*.5f,Vector2.zero,new Vector2(780,488),TideHUD.ink);
        var title=hud.Caption(hud.Box("标题",panel,new Vector2(.5f,1),new Vector2(0,-14),new Vector2(700,42),Color.clear),heading,27,TextAnchor.MiddleLeft);
        title.color=new Color32(239,208,143,255);
        body=hud.Caption(hud.Box("正文",panel,Vector2.one*.5f,new Vector2(0,4),new Vector2(704,324),Color.clear),"",23,TextAnchor.UpperLeft);
        body.lineSpacing=1.13f;
        body.supportRichText=false;
        pageCount=hud.Caption(hud.Box("页码",panel,new Vector2(0,0),new Vector2(40,17),new Vector2(115,43),Color.clear),"",18,TextAnchor.MiddleLeft);
        previous=ActionButton(panel,"上一页",323,128,"上一页",Previous,out _);
        ActionButton(panel,"继续阅读",469,180,"下一页（空格）",Next,out nextLabel);
        ActionButton(panel,"收起日志",660,88,"收起",Close,out _);
        if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        Canvas.ForceUpdateCanvases();
        Paginate();
        Display();
        return true;
    }

    Button ActionButton(RectTransform panel,string name,float x,float width,string label,System.Action action,out Text caption)
    {
        var rect=hud.Box(name,panel,Vector2.zero,new Vector2(x,17),new Vector2(width,43),new Color(.31f,.26f,.20f));
        caption=hud.Caption(rect,label,19,TextAnchor.MiddleCenter);
        hud.Click(rect,action);
        var button=rect.GetComponent<Button>();
        var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
        return button;
    }

    void Paginate()
    {
        // Ask the same text generator used by the visible label where each line starts.
        // No inferred character counts: Chinese punctuation, spaces and blank lines are retained.
        var settings=body.GetGenerationSettings(body.rectTransform.rect.size);
        settings.verticalOverflow=VerticalWrapMode.Overflow;
        var layout=new TextGenerator(text.Length);
        layout.Populate(text,settings);
        var lines=layout.lines;
        int first=0;int perPage=1;
        if(lines.Count>1)
        {
            float pitch=lines[0].topY-lines[1].topY;
            float height=body.rectTransform.rect.height*body.pixelsPerUnit;
            perPage=Mathf.Max(1,Mathf.FloorToInt((height-lines[0].height)/Mathf.Max(1,pitch))+1);
        }
        for(int i=perPage;i<lines.Count;i+=perPage)
        {
            int stop=Mathf.Min(lines[i].startCharIdx,text.Length);
            if(stop>first){pages.Add(text.Substring(first,stop-first));first=stop;}
        }
        if(first<text.Length || pages.Count==0)pages.Add(text.Substring(first));
    }

    void Display()
    {
        body.text=pages[currentPage].TrimEnd('\r','\n');
        pageCount.text=(currentPage+1)+" / "+pages.Count;
        previous.interactable=currentPage>0;
        nextLabel.text=currentPage+1<pages.Count?"下一页（空格）":"读完（空格）";
    }

    bool CanAct()=>Reading && !TidePause.BlocksInput && consumedFrame!=Time.frameCount;
    void Previous()
    {
        if(!CanAct()||currentPage==0)return;
        consumedFrame=Time.frameCount;currentPage--;Display();
    }
    void Next()
    {
        if(!CanAct())return;
        if(currentPage+1>=pages.Count){Close();return;}
        consumedFrame=Time.frameCount;currentPage++;Display();
    }
    void Close()
    {
        if(!CanAct())return;
        consumedFrame=Time.frameCount;reader.gameObject.SetActive(false);Object.Destroy(reader.gameObject);
    }
    public bool HandleReading()
    {
        if(consumedFrame==Time.frameCount)return true;
        if(!Reading)return false;
        if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Return))Next();
        else if(Input.GetKeyDown(KeyCode.LeftArrow))Previous();
        return true;
    }
}
