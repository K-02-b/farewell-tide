using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TideStory : MonoBehaviour {
 public int chapter; public SpriteRenderer background,hero,other,gate;
 public SpriteRenderer[] objects; public Vector2[] wifeMarks;
 public float left,right,ground; public string nextScene;
 [TextArea] public string[] lines;
 TideHUD ui; RectTransform dialog,photo,thumb,interactionPrompt; Text interactionLabel; Text dialogText,speaker;
 Sprite[] elderWalk,manIdle,walk,npc,wifeFront,glitch,rooms; Sprite field,broken; AudioSource audioOut; AudioClip step,paper,pick;
 bool[] taken=new bool[3];int count,meeting,elderLine; bool wifeReply, wifeLeaving; RectTransform fragment;bool speaking,viewing,portal,elderArrived,facingRight;
 UnityEngine.UI.Image portrait; string[] pages; int page;
 RectTransform movementHint; int lastDialogueInput=-1;
 readonly TideDialogueSkip dialogueSkip=new TideDialogueSkip();
 float elapsed,nextStep,nextAction,animTime;int target;
 void Start(){
  TideLook.Story(this);
  TideLook.Shadow(hero.transform,.5f,chapter==0?-1.55f:-.91f);
  if(other)TideLook.Shadow(other.transform,.34f,-.92f);
  manIdle=TideArt.Frames("ManIdle");walk=TideArt.Frames("ManWalk");npc=TideArt.Frames(chapter==1?"Wife":"Elder");glitch=TideArt.Frames("FieldGlitch");
  if(chapter==3)elderWalk=TideArt.Frames("ElderWalk");
  if(chapter==1)wifeFront=TideArt.Frames("WifeFront");
  rooms=new Sprite[4];for(int i=0;i<4;i++)rooms[i]=TideArt.Pic("Art/Room"+i);field=TideArt.Pic("Art/Field");broken=TideArt.Pic("Art/FieldBroken");
  audioOut=gameObject.AddComponent<AudioSource>();step=Sound(chapter==0?"Wood":"Soil");paper=Sound("Paper");pick=Sound("Pickup");
  ui=new TideHUD(true);
  if(chapter==0){
   movementHint=ui.Panel("首次移动提示",.5f,1,0,-26,460,42,TideHUD.ink);
   ui.Caption(movementHint,"A、D 或左右方向键移动，Esc 暂停",20,TextAnchor.MiddleCenter);
  }
  interactionPrompt=ui.Panel("交互提示",.5f,.5f,0,0,124,34,TideHUD.ink);
  interactionLabel=ui.Caption(interactionPrompt,"[E] 交互",18,TextAnchor.MiddleCenter);
  interactionPrompt.gameObject.SetActive(false);
  dialog=ui.Panel("对话",.5f,1,0,-24,900,156,TideHUD.ink);speaker=ui.Caption(ui.Box("名字",dialog,new Vector2(0,1),new Vector2(134,-16),new Vector2(630,28),Color.clear),"",19,TextAnchor.MiddleLeft);
  speaker.color=new Color(.92f,.77f,.46f);dialogText=ui.Caption(ui.Box("台词",dialog,Vector2.zero,new Vector2(134,40),new Vector2(728,64),Color.clear),"",24,TextAnchor.MiddleLeft);
  dialogText.fontSize=23;dialogText.lineSpacing=1.25f;
  ui.Border(dialog,new Color(.73f,.60f,.38f,.65f));
  ui.DialogueContinue(dialog);
  portrait=ui.Box("头像",dialog,new Vector2(0,.5f),new Vector2(30,0),new Vector2(80,88),Color.white,manIdle[0]).GetComponent<Image>();
  ui.Click(dialog,Close);dialog.gameObject.SetActive(false);
  fragment=ui.Panel("记忆碎片",0,0,8,8,400,122,Color.white,TideArt.Pic("Art/Fragment2"));ui.Click(fragment,()=>fragment.gameObject.SetActive(false));fragment.gameObject.SetActive(false);
  thumb=ui.Panel("查看合照",1,0,-24,22,88,70,Color.white,TideArt.Pic("Art/Frame"));ui.Click(thumb,OpenPhoto);thumb.gameObject.SetActive(false);
  photo=ui.Panel("照片",.5f,.5f,0,0,1280,720,new Color(.03f,.04f,.05f,.91f));ui.Box("合照",photo,new Vector2(.5f,.5f),Vector2.zero,new Vector2(850,490),Color.white,TideArt.Pic("Art/Photo"));ui.Click(photo,ClosePhoto);photo.gameObject.SetActive(false);
  if(chapter==3 && glitch.Length>0)TideArt.Fit(background,glitch[0],9);if(gate)gate.gameObject.SetActive(false);Frame();
 }
 AudioClip Sound(string n)=>Resources.Load<AudioClip>("Tide/Sound/"+n);
 void OpenPhoto(){
  if(viewing||TidePause.BlocksInput||lastDialogueInput==Time.frameCount)return;
  lastDialogueInput=Time.frameCount;viewing=true;
  interactionPrompt.gameObject.SetActive(false);photo.gameObject.SetActive(true);
  // Return also submits to the selected UI button. Keep focus on the open photo.
  var button=photo.GetComponent<Button>();
  button.navigation=new Navigation{mode=Navigation.Mode.None};
  button.Select();
  audioOut.PlayOneShot(paper,.5f);
 }
 void ClosePhoto(){
  if(!viewing||TidePause.BlocksInput||lastDialogueInput==Time.frameCount)return;
  lastDialogueInput=Time.frameCount;viewing=false;
  photo.gameObject.SetActive(false);nextAction=Time.time+.15f;
 }
 void Talk(string name,string line){speaking=true;lastDialogueInput=Time.frameCount;if(movementHint)movementHint.gameObject.SetActive(false);interactionPrompt.gameObject.SetActive(false);dialog.gameObject.SetActive(true);speaker.text=name;page=0;
  var parts=new System.Collections.Generic.List<string>();int start=0;
  while(start<line.Length){int length=Mathf.Min(58,line.Length-start);if(start+length<line.Length){int cut=-1;for(int j=start+length-1;j>start+20;j--)if("，。！!？?……".Contains(line[j].ToString())){cut=j;break;}if(cut>=0)length=cut-start+1;}parts.Add(line.Substring(start,length));start+=length;}pages=parts.ToArray();dialogText.text=pages[0];
  bool hasSpeaker=!string.IsNullOrEmpty(name);
  portrait.gameObject.SetActive(hasSpeaker);
  if(hasSpeaker){
   var face=name=="乔西"?manIdle[0]:TideArt.Frames(name=="华娜"?"Wife":"Elder")[0];
   var rect=face.rect;rect.y+=rect.height*.57f;rect.height*=.43f;portrait.sprite=Sprite.Create(face.texture,rect,new Vector2(.5f,.5f),face.pixelsPerUnit);
  }
  dialog.sizeDelta=new Vector2(900,156);
  var body=(RectTransform)dialogText.transform.parent;
  body.anchoredPosition=hasSpeaker?new Vector2(134,40):new Vector2(30,44);
  body.sizeDelta=new Vector2(hasSpeaker?728:840,68);}
 void Close(){if(viewing||!speaking||TidePause.BlocksInput||lastDialogueInput==Time.frameCount)return;lastDialogueInput=Time.frameCount;if(pages!=null&&page+1<pages.Length){dialogText.text=pages[++page];return;}if(chapter==3&&elderLine+1<lines.Length){elderLine++;Talk(ElderSpeaker(),lines[elderLine]);return;}
  if(chapter==1&&meeting==2&&!wifeReply){wifeReply=true;StartCoroutine(TideLook.FadeOut(other));Talk("乔西","我■你，■■的，花");return;}
  speaking=false;dialog.gameObject.SetActive(false);nextAction=Time.time+.16f;
 if(chapter==1){
  if(meeting==2) meeting++;
  else {
   wifeLeaving=true;
   StartCoroutine(TideLook.FadeOut(other,()=>{
    meeting++;
    other.transform.position=wifeMarks[meeting];
    other.gameObject.SetActive(true);
    wifeLeaving=false;
   }));
  }
 }
 if(chapter==3){portal=true;gate.gameObject.SetActive(true);StartCoroutine(TideLook.FadeOut(other));}}
 string ElderSpeaker(){if(elderLine>=11)return string.Empty;return elderLine==10||elderLine==9||elderLine==8||elderLine==6?"艾德曼":elderLine%2==0?"老人":"乔西";}
 void Frame(){TideArt.CameraFrame(Camera.main,16,9);}
 void UpdateInteractionPrompt(){
  bool visible=target>=0&&!speaking&&!viewing&&Time.time>=nextAction;
  interactionPrompt.gameObject.SetActive(visible);if(!visible)return;
  SpriteRenderer item=target<3?objects[target]:target==3||target==4?other:chapter==3?gate:null;
  Vector3 world=item?new Vector3(item.bounds.max.x+.12f,item.bounds.center.y,0):new Vector3(right-.3f,ground+1,0);
  bool conversation=target==3||target==4;
  interactionLabel.text=conversation?"[E] 对话":target==5&&chapter==0?"[E] 出门":"[E] 交互";
  var screen=Camera.main.WorldToScreenPoint(world);var viewport=Camera.main.pixelRect;
  float scale=ui.root.GetComponent<Canvas>().scaleFactor;
  float halfWidth=interactionPrompt.sizeDelta.x*scale*.5f;
  if(item){
   screen.x+=halfWidth;
   if(screen.x+halfWidth>viewport.xMax-8*scale)
    screen.x=Camera.main.WorldToScreenPoint(new Vector3(item.bounds.min.x-.12f,item.bounds.center.y,0)).x-halfWidth;
  }
  screen.x=Mathf.Clamp(screen.x,viewport.xMin+halfWidth+8*scale,viewport.xMax-halfWidth-8*scale);
  screen.y=Mathf.Clamp(screen.y,viewport.yMin+24*scale,viewport.yMax-24*scale);
  Vector2 point;RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root,screen,null,out point);
  interactionPrompt.anchoredPosition=point;
 }
 void Update(){
  if(TidePause.BlocksInput){dialogueSkip.Reset();return;}
  float dt=Time.deltaTime;elapsed+=dt;bool action=Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Return);
  if(viewing){dialogueSkip.Reset();if(action||Input.GetKeyDown(KeyCode.Space))ClosePhoto();return;}
  float dx=speaking?0:Input.GetAxisRaw("Horizontal");Vector3 p=hero.transform.position;p.x=Mathf.Clamp(p.x+dx*dt*4,left,right);hero.transform.position=p;
  if(dx!=0){if(movementHint&&movementHint.gameObject.activeSelf)movementHint.gameObject.SetActive(false);facingRight=dx>0;animTime+=dt;if(Time.time>=nextStep){audioOut.PlayOneShot(step,chapter==0?TideLook.WoodStepVolume:.28f);nextStep=Time.time+.38f;}}
  hero.sprite=dx==0?manIdle[(int)(elapsed*6)%manIdle.Length]:walk[(int)(animTime*9)%walk.Length];hero.flipX=dx!=0&&facingRight;
  if(other&&other.gameObject.activeSelf){
   bool stepping=chapter==3&&!elderArrived;
   var frames=stepping?elderWalk:chapter==1&&meeting==1?wifeFront:npc;
   other.sprite=frames[(int)(elapsed*(stepping?10:6))%frames.Length];
   if(chapter==3)TideArt.Fit(other,other.sprite,1.9f);
   if(chapter==1)other.flipX=meeting>=2;
  }
  if(chapter==2)background.sprite=(p.x>=0&&p.x<4.6f)?broken:field;
  if(chapter==3){background.sprite=glitch[(int)(elapsed*2)%glitch.Length];if(!elderArrived){other.transform.position=Vector3.MoveTowards(other.transform.position,new Vector3(.4f,ground,0),1.5f*dt);elderArrived=Vector3.Distance(other.transform.position,new Vector3(.4f,ground,0))<.01f;}}
  Frame();target=-1;
  if(chapter==0)for(int i=0;i<3;i++){
   float itemX=Mathf.Clamp(objects[i].transform.position.x,left-.8f,right+.8f);
   if(!taken[i]&&Mathf.Abs(p.x-itemX)<1.4f){target=i;break;}
  }
  if(chapter==1&&!wifeLeaving&&meeting<3&&Mathf.Abs(p.x-other.transform.position.x)<1.4f)target=3;
  if(chapter==3&&elderArrived&&!portal&&Mathf.Abs(p.x-other.transform.position.x)<1.4f)target=4;
  if(p.x>right-1){bool open=chapter==0?count==3:chapter==1?meeting==3:chapter==2||portal;
   if(open)target=5;}
  UpdateInteractionPrompt();
  HandleInteraction(action,Input.GetKeyDown(KeyCode.Space));
  if(dialogueSkip.ShouldRepeat(speaking))Close();
 }
 void HandleInteraction(bool action,bool space){
  if(speaking){if(action||space)Close();return;}
  bool startConversation=space&&(target==3||target==4);
  if((!action&&!startConversation)||Time.time<nextAction)return;
  if(target>=0&&target<=2){taken[target]=true;count++;background.sprite=rooms[count];TideLook.Background(this);audioOut.PlayOneShot(pick);Talk("乔西",lines[target]);var f=TideArt.Pic("Art/Fragment"+new[]{2,0,1}[count-1]);fragment.GetComponent<Image>().sprite=f;fragment.sizeDelta=new Vector2(400,400*f.bounds.size.y/f.bounds.size.x);fragment.gameObject.SetActive(true);if(target==2)thumb.gameObject.SetActive(true);}
  else if(target==3)Talk("华娜",lines[meeting]);else if(target==4)Talk(ElderSpeaker(),lines[0]);else if(target==5)SceneManager.LoadScene(nextScene);
 }
}

