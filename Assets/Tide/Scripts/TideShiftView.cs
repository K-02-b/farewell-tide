using UnityEngine;
using UnityEngine.UI;

// The camera pulls back from the level to reveal the player's limited editing tools.
public sealed class TideShiftView {
 const float Duration=.6f,PanelFraction=.16f;
 readonly TideHUD ui;
 readonly float width,height,left,bottom;
 readonly RectTransform tools,bar,dragIcon;
 readonly RectTransform[] buttons=new RectTransform[3];
 readonly Image[] buttonImages=new Image[3];
 readonly RectTransform instructionArea;
 readonly Text instruction;
 readonly CanvasGroup group;
 readonly SpriteRenderer background;
 readonly Sprite[] platformSprites;
 float progress;
 bool opening;
 public float Blend {get;private set;}
 const float SlowMotionSpeed=.08f;
 public float GameSpeed=>opening?SlowMotionSpeed:Mathf.Lerp(1,SlowMotionSpeed,Blend);
 public bool Ready=>progress>=.999f;

 public TideShiftView(TideHUD hud,float w,float h,RectTransform statusBar,Sprite[] platformSprites) {
  ui=hud;width=w;height=h;bar=statusBar;this.platformSprites=platformSprites;
  // Grow both axes together so the level and the revealed panel share a viewport.
  left=w*PanelFraction/(1-PanelFraction);bottom=h*PanelFraction/(1-PanelFraction);
  background=TideArt.Place("漏洞控制面板",TideArt.Pic("UI/ControlPanel"),(w-left)/2,(h-bottom)/2,h+bottom,-30);
  background.transform.localScale=new Vector3((w+left)/background.sprite.bounds.size.x,(h+bottom)/background.sprite.bounds.size.y,1);
  tools=ui.Panel("漏洞权限工具",.5f,.5f,0,0,0,0,Color.clear);
  group=tools.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
  instructionArea=ui.Box("放置操作",tools,Vector2.one*.5f,Vector2.zero,new Vector2(600,40),Color.clear);
  instruction=ui.Caption(instructionArea,"",17,TextAnchor.MiddleLeft);
  for(int i=0;i<3;i++) {
   buttons[i]=ui.Box("平台素材 "+i,tools,Vector2.one*.5f,Vector2.zero,new Vector2(120,82),Color.white,platformSprites[i]);
   buttonImages[i]=buttons[i].GetComponent<Image>();
   var label=ui.Box("平台名称",buttons[i],new Vector2(.5f,0),new Vector2(0,-6),new Vector2(150,25),Color.clear);
   ui.Caption(label,new[]{"普通平台","高跳平台","冲刺平台"}[i],15,TextAnchor.MiddleCenter);
  }
  dragIcon=ui.Box("拖动素材",ui.root,Vector2.one*.5f,Vector2.zero,new Vector2(100,44),Color.white,platformSprites[0]);
  dragIcon.gameObject.SetActive(false);
  Tick(false,0,0);
 }
 Vector2 CanvasPoint(Vector2 screen) {Vector2 point;RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root,screen,null,out point);return point;}
 public void Tick(bool expanded,int charge,float cooldown) {
  opening=expanded;
  progress=Mathf.MoveTowards(progress,expanded?1:0,Time.unscaledDeltaTime/Duration);
  Blend=Mathf.SmoothStep(0,1,progress);
  var c=Camera.main;TideArt.FrameViewport(c,width/height);
  c.orthographic=true;c.orthographicSize=Mathf.Lerp(height,height+bottom,Blend)*.5f;
  c.transform.position=new Vector3((width-left*Blend)*.5f,(height-bottom*Blend)*.5f,-10);
  background.enabled=progress>0;
  group.alpha=Blend;
  float canvasScale=ui.root.GetComponent<Canvas>().scaleFactor;
  float sidePixels=left*c.pixelHeight/(c.orthographicSize*2);
  instructionArea.sizeDelta=new Vector2(width*.51f*c.pixelHeight/(2*c.orthographicSize*canvasScale),42);
  instructionArea.anchoredPosition=CanvasPoint(c.WorldToScreenPoint(new Vector3(width*.23f,-bottom*.58f)));
  for(int i=0;i<3;i++) {
   buttons[i].anchoredPosition=CanvasPoint(c.WorldToScreenPoint(new Vector3(-left*.5f,height*(.78f-i*.28f),0)));
   buttons[i].sizeDelta=new Vector2(Mathf.Min(160,sidePixels*.82f/canvasScale),76);
   buttonImages[i].color=charge>0&&cooldown<=0?Color.white:new Color(.5f,.57f,.63f,.7f);
  }
  bar.anchorMin=bar.anchorMax=bar.pivot=Vector2.one*.5f;
  var normalStatus=CanvasPoint(c.WorldToScreenPoint(new Vector3(0,height,0)))+new Vector2(64,-18);
  var panelStatus=CanvasPoint(c.WorldToScreenPoint(new Vector3(-left*.5f,height,0)))+new Vector2(0,-18);
  bar.anchoredPosition=Vector2.Lerp(normalStatus,panelStatus,Blend);
  bar.sizeDelta=new Vector2(100,18);
 }
 public void CloseImmediately(){progress=0;ShowInstruction("");Tick(false,0,0);}
 public void ShowInstruction(string value){if(instruction.text!=value)instruction.text=value;}
 public int Pick(Vector2 mouse,bool allowDuringReveal) {
  if(!Ready&&(!allowDuringReveal||Blend<.2f))return -1;
  for(int i=0;i<3;i++)if(RectTransformUtility.RectangleContainsScreenPoint(buttons[i],mouse))return i;
  return -1;
 }
 public bool InLevel(Vector2 mouse) {
  if(!Camera.main.pixelRect.Contains(mouse))return false;
  Vector2 p=Camera.main.ScreenToWorldPoint(mouse);return p.x>=0&&p.x<=width&&p.y>=0&&p.y<=height;
 }
 public void Drag(int kind,Vector2 mouse,bool inLevel) {
  dragIcon.gameObject.SetActive(kind>=0&&!inLevel);
  if(kind<0)return;
  dragIcon.anchoredPosition=CanvasPoint(mouse);dragIcon.GetComponent<Image>().sprite=platformSprites[kind];
 }
}
