using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class TideJournal
{
    const string CollectionKey = "Tide.Journal.Collection";
    static string[] CollectedIds() => PlayerPrefs.GetString(CollectionKey, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    public static bool HasAny => PlayerPrefs.GetString(CollectionKey, "").Length > 0;
    public static bool Has(TextAsset document) => Array.IndexOf(CollectedIds(), document.name) >= 0;
    public static void Collect(TextAsset document)
    {
        if (Has(document)) return;
        var ids = new List<string>(CollectedIds()) { document.name };
        PlayerPrefs.SetString(CollectionKey, string.Join("|", ids));
        PlayerPrefs.Save();
    }

    readonly TideHUD hud;
    readonly List<string> pages = new List<string>();
    readonly List<Selectable> backgroundControls = new List<Selectable>();
    readonly Action closed;
    RectTransform overlay, panel;
    Text body, pageCount, nextLabel;
    Button previous;
    bool reading, fromList;
    float previousSpeed;
    int page, consumedFrame;
    public bool Open { get; private set; }

    public TideJournal(Action closed)
    {
        this.closed = closed;
        hud = new TideHUD();
        hud.root.GetComponent<Canvas>().sortingOrder = 280;
    }

    public void Show(TextAsset document)
    {
        Open=true;
        foreach(var control in Selectable.allSelectablesArray)
        {
            if(!control.interactable)continue;
            backgroundControls.Add(control);
            control.interactable=false;
        }
        overlay = hud.Panel("日志", .5f,.5f,0,0,0,0,new Color(0,0,0,.65f));
        overlay.anchorMin=Vector2.zero; overlay.anchorMax=Vector2.one;
        overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        var dim=overlay.GetComponent<Image>(); dim.sprite=null; dim.raycastTarget=true;
        previousSpeed = Time.timeScale;
        Time.timeScale = 0;
        if(document) Read(document, false); else ListEntries();
    }

    static string Title(TextAsset document)
    {
        int end=document.text.IndexOf('\n');
        return end<0 ? document.name : document.text.Substring(0,end).TrimEnd('\r');
    }

    void SetPanel(string name)
    {
        consumedFrame=Time.frameCount;
        if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        if(panel) { panel.gameObject.SetActive(false); UnityEngine.Object.Destroy(panel.gameObject); }
        panel=hud.Box(name,overlay,Vector2.one*.5f,Vector2.zero,new Vector2(780,488),TideHUD.ink);
        hud.Border(panel,new Color(.73f,.60f,.38f,.65f));
    }

    void Heading(string title)
    {
        var rect=hud.Box("标题",panel,new Vector2(.5f,1),new Vector2(0,-14),new Vector2(700,42),Color.clear);
        hud.Caption(rect,title,27,TextAnchor.MiddleLeft).color=new Color32(239,208,143,255);
    }

    void ListEntries()
    {
        SetPanel("日志目录"); reading=false; Heading("已收集的日志");
        Button first=null;
        float y=338;
        foreach(string id in CollectedIds())
        {
            var doc=Resources.Load<TextAsset>("Tide/Journals/"+id);
            if(!doc) continue;
            var button=ActionButton(Title(doc),50,y,680,50,()=>Read(doc,true),out _);
            if(!first) first=button;
            y-=62;
        }
        ActionButton("收起",640,17,108,43,Close,out _);
        if(first && EventSystem.current) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    void Read(TextAsset document,bool fromList)
    {
        SetPanel("日志纸面"); reading=true; this.fromList=fromList;
        Heading(Title(document));
        var source=document.text.Replace("\r\n","\n");
        int split=source.IndexOf('\n');
        var text=split<0 ? source : source.Substring(split+1);
        body=hud.Caption(hud.Box("正文",panel,Vector2.one*.5f,new Vector2(0,4),new Vector2(704,324),Color.clear),"",23,TextAnchor.UpperLeft);
        body.lineSpacing=1.13f;
        body.supportRichText=false;
        pageCount=hud.Caption(hud.Box("页码",panel,Vector2.zero,new Vector2(40,17),new Vector2(115,43),Color.clear),"",18,TextAnchor.MiddleLeft);
        previous=ActionButton("上一页",320,17,116,43,Previous,out _);
        ActionButton("下一页",448,17,116,43,Next,out nextLabel);
        ActionButton(fromList ? "返回目录" : "收起",580,17,160,43,FinishReading,out _);
        Canvas.ForceUpdateCanvases(); pages.Clear(); page=0; Paginate(text); Display();
    }

    Button ActionButton(string label,float x,float y,float width,float height,Action action,out Text caption)
    {
        var rect=hud.Box(label,panel,Vector2.zero,new Vector2(x,y),new Vector2(width,height),new Color(.31f,.26f,.20f));
        caption=hud.Caption(rect,label,19,TextAnchor.MiddleCenter);
        hud.Click(rect,()=>{if(CanAct())action();});
        var button=rect.GetComponent<Button>();
        if(reading)button.navigation=new Navigation {mode=Navigation.Mode.None};
        return button;
    }

    void Paginate(string text)
    {
        var settings=body.GetGenerationSettings(body.rectTransform.rect.size);
        settings.verticalOverflow=VerticalWrapMode.Overflow;
        var layout=new TextGenerator(text.Length);
        layout.Populate(text,settings);
        var lines=layout.lines;
        int first=0, perPage=1;
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
        body.text=pages[page].TrimEnd('\r','\n');
        pageCount.text=(page+1)+" / "+pages.Count;
        previous.interactable=page>0;
        nextLabel.text=page+1<pages.Count?"下一页":"读完";
    }

    bool CanAct()=>Open && consumedFrame!=Time.frameCount;
    void Previous()
    {
        if(page==0)return;
        consumedFrame=Time.frameCount; page--; Display();
    }
    void Next()
    {
        if(page+1>=pages.Count){FinishReading();return;}
        consumedFrame=Time.frameCount; page++; Display();
    }
    void FinishReading(){if(fromList)ListEntries();else Close();}
    public void Close()
    {
        if(!Open)return;
        Open=false;
        consumedFrame=Time.frameCount; Time.timeScale=previousSpeed;
        if(overlay){overlay.gameObject.SetActive(false); UnityEngine.Object.Destroy(overlay.gameObject);}
        foreach(var control in backgroundControls)if(control)control.interactable=true;
        backgroundControls.Clear();
        if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        closed();
    }

    public void HandleKeys()
    {
        if(!CanAct())return;
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Tab))Close();
        else if(reading && (Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Return)))Next();
        else if(reading && Input.GetKeyDown(KeyCode.LeftArrow))Previous();
    }
}

