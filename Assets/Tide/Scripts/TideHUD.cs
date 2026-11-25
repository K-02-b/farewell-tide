using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class TideArt {
 public static Sprite Pic(string name) => Resources.Load<Sprite>("Tide/"+name);
 public static Sprite[] Frames(string name) => Resources.LoadAll<Sprite>("Tide/Anim/"+name).OrderBy(s=>s.name).ToArray();
 public static void Fit(SpriteRenderer r,Sprite s,float h) { if(!s)return; r.sprite=s; float scale=h/s.bounds.size.y; r.transform.localScale=new Vector3(scale,scale,1); }
 public static SpriteRenderer Place(string name,Sprite s,float x,float y,float h,int layer) {var r=new GameObject(name).AddComponent<SpriteRenderer>(); r.transform.position=new Vector3(x,y,0);r.sortingOrder=layer; Fit(r,s,h);return r;}
 public static void FrameViewport(Camera c,float wanted) {
  int w=c.targetTexture?c.targetTexture.width:Screen.width, h=c.targetTexture?c.targetTexture.height:Screen.height;
  if(w<1||h<1)return;
  int vw=Mathf.Clamp(Mathf.RoundToInt(h*wanted),1,w),vh=Mathf.Clamp(Mathf.RoundToInt(w/wanted),1,h);
  c.rect=new Rect(((w-vw)/2)/(float)w,((h-vh)/2)/(float)h,vw/(float)w,vh/(float)h);
  if(Application.isPlaying&&(vw!=w||vh!=h)&&!c.GetComponent<TideViewportEdges>())
   c.gameObject.AddComponent<TideViewportEdges>();
 }
 public static void CameraFrame(Camera c,float w,float h,float cx=0,float cy=0) {
  FrameViewport(c,w/h);
  c.orthographic=true;c.orthographicSize=h/2;c.transform.position=new Vector3(cx,cy,-10);
 }
}
public class TideHUD {
 public readonly RectTransform root;
 readonly Font font; public static readonly Color ink=new Color(.12f,.105f,.085f,.96f);
 readonly bool narrative;
 public TideHUD(bool narrative=false){
  this.narrative=narrative;
  if(Application.isPlaying)TidePause.EnsureForScene();
  var canvas=new GameObject("界面",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var sc=canvas.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1280,720);sc.matchWidthOrHeight=.5f;
  root=(RectTransform)canvas.transform;font=Resources.Load<Font>("Tide/Text");
  if(!EventSystem.current)new GameObject("输入",typeof(EventSystem),typeof(StandaloneInputModule));
  if(narrative && Application.isPlaying)TideTimeline.ShowWhenEntering(this);
 }
 public RectTransform Box(string name,RectTransform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Sprite sprite=null) {
  var g=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;
  var i=g.GetComponent<Image>();i.color=color;i.sprite=sprite;i.preserveAspect=sprite!=null;i.raycastTarget=false;
  if(sprite==null && color.a>.1f){i.sprite=TideArt.Pic("UI/Panel");i.type=Image.Type.Sliced;}
  return r;
 }
 public Text Caption(RectTransform parent,string value,int size,TextAnchor align) {
  var g=new GameObject("文字",typeof(RectTransform),typeof(Text));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,2);r.offsetMax=new Vector2(-12,-2);
  var t=g.GetComponent<Text>();t.font=font;t.fontSize=size;t.alignment=align;t.text=value;t.color=new Color(.99f,.96f,.88f);t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;
 }
 public void Border(RectTransform r,Color color){
  var line=Box("上边线",r,new Vector2(.5f,1),new Vector2(0,-1),new Vector2(r.sizeDelta.x-24,1),color);line.GetComponent<Image>().sprite=null;
 }
 public void Click(RectTransform r,Action action){r.GetComponent<Image>().raycastTarget=true;Button b=narrative?(Button)r.gameObject.AddComponent<TideNarrativeButton>():r.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>action());}
 public RectTransform Panel(string name,float ax,float ay,float x,float y,float w,float h,Color c,Sprite s=null)=>Box(name,root,new Vector2(ax,ay),new Vector2(x,y),new Vector2(w,h),c,s);
}
