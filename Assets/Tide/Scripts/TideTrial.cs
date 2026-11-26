using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class TideTrial : MonoBehaviour {
 public TextAsset layout;public SpriteRenderer hero,exit;public SpriteRenderer[] orbs;public string nextScene;
 public int number; public Vector2 spawn;
 public Vector2 fragmentPosition;
 public TextAsset fragmentDocument;
 TideTrialFragment fragment;
 RectTransform journalButton;
 TextAsset pendingJournal;
 bool journalRequested;
 public bool spikeHazards;
 public SpriteRenderer startingPad;
 [System.Serializable] public struct PlatformSpot {
  public Vector2 center; public int kind; public float width;
  public PlatformSpot(float x,float y,int kind,float width=2.25f){center=new Vector2(x,y);this.kind=kind;this.width=width;}
  // Older scenes may still serialize the free-placement width for a marked spot.
  public float Width=>width>2f?width:2.25f;
 }
 public PlatformSpot[] placementHints;
 readonly List<SpriteRenderer> hintViews=new List<SpriteRenderer>();
 bool MarkedRoute=>placementHints!=null&&placementHints.Length>0;
 [Min(1)] public int failuresBeforeHint=3;
 int failures,hintActionFrame=-1;
 RectTransform hintButton,hintOverlay;
 Text hintButtonLabel;
 RectTransform[] hintPlatforms;
 bool viewingHint;
 public float startingPadLifetime;
 float padTime=-1;
 float jumpBuffer,ledgeGrace,horizontalSpeed;
 TideTrialFeedback feedback;
 TideFlyingSpikes flyingSpikes;
 TidePatrols patrols;
 SpriteRenderer decoy;
 Sprite errorIcon;
 Vector2 decoyPad;
 bool decoyArmed,decoyTriggered;
 float decoyAge;
 bool playerJump,jumpReleased,airborne;
 float takeoffAge=float.PositiveInfinity,landingAge,landingDuration;
 readonly HashSet<Collider2D> spikes=new HashSet<Collider2D>();
 string[] rows; int w,h,charge,drag=-1,support=-1;float vy,dash,dxDash,cooldown,clock; bool admin,finished;
 // In the opening pair each bare rise is 3 tiles, split by a player-placed red pad.
 // Older rooms retain their longer ordinary jump envelope.
 float FullJumpHeight=>number<=2?2.8f:4.45f;
 float RiseTime=>number<=2?.46f:.62f;
 float RiseGravity=>2f*FullJumpHeight/(RiseTime*RiseTime);
 float JumpSpeed=>RiseGravity*RiseTime;
 const float FallLimit=19f,FastFallLimit=25f;
 bool fastFallHeld,spawnFacingRight;
 double respawnAt;
 LandingCheckpoint checkpoint;
 struct FixedLanding { public Vector2 center; public float halfWidth,surfaceY; }
 readonly List<FixedLanding> fixedLandings=new List<FixedLanding>();
 int touchedLanding=-1;
 Vector2 pos;const float Half=.29f,Body=1.24f;
 readonly List<Pad> pads=new List<Pad>();struct Pad {public Vector2 center;public float halfWidth;public int type;public SpriteRenderer view;}
 [System.Serializable] struct PlacedPlatformState {public Vector2 center;public float width;public int type;}
 bool[] initialOrbs;
 [System.Serializable] sealed class LandingCheckpoint{
  public int version;
  public bool movedExit;
  public Vector2 feet;
  public bool facingRight,hiddenBrickShown;
  public PlacedPlatformState[] placedPlatforms;
  public bool[] visibleOrbs;
  public int charge;
  public float cooldown,padTime,patrolDistance;
  public bool startingPadEnabled;
  public Color startingPadColor;
  public TideFlyingSpikes.RetryState[] flyingSpikes;
  public bool decoyEnabled,decoyArmed,decoyTriggered;
  public float decoyAge;
  public Vector2 decoyPad;
  public Vector3 decoyPosition,decoyScale;
  public Color decoyColor;
 }
 const string LandingKeyPrefix="Tide.Landing.v2.",OldLandingKeyPrefix="Tide.Landing.v1.";
 static string resumeLandingScene;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void ResetResumeRequest(){resumeLandingScene=null;}
 public static void ResumeSavedLanding(string sceneName){resumeLandingScene=sceneName;}
 public static void ClearSavedLanding(string sceneName){
  if(resumeLandingScene==sceneName)resumeLandingScene=null;
  var key=LandingKeyPrefix+sceneName;
  var oldKey=OldLandingKeyPrefix+sceneName;
  if(!PlayerPrefs.HasKey(key)&&!PlayerPrefs.HasKey(oldKey))return;
  PlayerPrefs.DeleteKey(key);
  PlayerPrefs.DeleteKey(oldKey);
  PlayerPrefs.Save();
 }
 readonly HashSet<Collider2D> terrain=new HashSet<Collider2D>();readonly Collider2D[] hits=new Collider2D[64];
 readonly RaycastHit2D[] surfaceHits=new RaycastHit2D[16];
 TideHUD ui;TideShiftView shiftView;RectTransform energyFill;SpriteRenderer ghost;
 RectTransform welcome,storyExit;
 int openedExitFrame;
 bool exitRelocated;
 float exitTravelLeft;
 Vector3 falseExitPosition;
 // The second red foothold in Trial10 is reachable again from the restored starting route.
 Vector3 RelocatedExitPosition=>new Vector3(placementHints[1].center.x,placementHints[1].center.y+.95f,falseExitPosition.z);
 const float ExitTravelTime=1.15f;
 MeshRenderer footShadow;
 Tilemap hiddenBrickMap;
 TileBase hiddenBrickTile;
 Vector3Int hiddenBrickCell;
 float hiddenBrickBottom,hiddenBrickLeft;
 bool hiddenBrickShown;
 Sprite[] idle,walking,jumpFrames;Sprite[] platformSprites; AudioSource audioOut;AudioClip pickup,land,dashSound;
 void Start(){rows=layout.text.Trim().Split('\n');w=rows[0].Trim().Length;h=rows.Length;pos=spawn;spawnFacingRight=hero.flipX;
 foreach(var root in gameObject.scene.GetRootGameObjects())if(root.GetComponent<Grid>()&&root.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>()) {
  foreach(var tilemap in root.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapCollider2D>())tilemap.ProcessTilemapChanges();
  foreach(var collider in root.GetComponentsInChildren<Collider2D>()){
   terrain.Add(collider);
   if(spikeHazards && collider is PolygonCollider2D) spikes.Add(collider);
  }
 }
 Physics2D.SyncTransforms();
 FindFixedLandings();
 PrepareHiddenBrick();
 flyingSpikes=new TideFlyingSpikes(number,spikes,transform,JumpSpeed);
 patrols=TidePatrols.Create(number,transform);
 idle=TideArt.Frames("ManSide");walking=TideArt.Frames("ManWalk");jumpFrames=TideArt.Frames("ManJump");platformSprites=new[]{TideArt.Pic("Art/PlatformNormal"),TideArt.Pic("Art/PlatformBounce"),TideArt.Pic("Art/PlatformDash")};
 audioOut=gameObject.AddComponent<AudioSource>();pickup=Resources.Load<AudioClip>("Tide/Sound/Pickup");land=Resources.Load<AudioClip>("Tide/Sound/Land");
 dashSound=Resources.Load<AudioClip>("Tide/Sound/Teleport");
 TideLook.Shadow(hero.transform,.25f,-.65f);
 footShadow=hero.GetComponentInChildren<MeshRenderer>();
 ui=new TideHUD();
 if(fragmentDocument) fragment=new TideTrialFragment(transform,fragmentPosition,fragmentDocument);
 var bar=ui.Panel("关卡能量",.5f,1,0,-16,100,18,Color.clear);
 ui.Box("能量球",bar,new Vector2(0,.5f),Vector2.zero,new Vector2(16,16),Color.white,TideArt.Pic("Art/Orb"));
 var track=ui.Box("能量槽",bar,new Vector2(0,.5f),new Vector2(20,0),new Vector2(76,9),Color.white);
 var trackImage=track.GetComponent<Image>();trackImage.sprite=null;trackImage.type=Image.Type.Simple;trackImage.color=new Color(.13f,.17f,.2f,.96f);
 energyFill=ui.Box("能量",track,new Vector2(0,.5f),new Vector2(2,0),new Vector2(0,5),Color.white);
 var fillImage=energyFill.GetComponent<Image>();fillImage.sprite=null;fillImage.type=Image.Type.Simple;fillImage.color=new Color(1,.78f,.27f);
 shiftView=new TideShiftView(ui,w,h,bar,platformSprites);
 if(MarkedRoute)feedback=new TideTrialFeedback(hero,transform);
 initialOrbs=CaptureOrbs();
 RestoreStartingPad();
 if(number==1&&placementHints!=null)foreach(var spot in placementHints){
  var view=Make("建议落点",spot.center,spot.kind,spot.Width);view.color=new Color(1,1,1,.36f);view.enabled=false;hintViews.Add(view);
 }
 ghost=Make("放置预览",Vector2.zero,0);ghost.enabled=false;RenderHero(false);
 shiftView.Tick(false,charge,cooldown);
 if(MarkedRoute){
  hintButton=ui.Panel("查看放置提示图",1,1,-16,-12,192,39,TideHUD.ink);
  hintButtonLabel=ui.Caption(hintButton,"H  查看提示图",18,TextAnchor.MiddleCenter);
  ui.Click(hintButton,ToggleRouteHint);
  hintButton.GetComponent<Button>().navigation=new Navigation{mode=Navigation.Mode.None};
  hintButton.gameObject.SetActive(TideGuideBook.Unlocked(number));
 }
 journalButton=ui.Panel("查看日志",1,1,-16,-57,192,39,TideHUD.ink);
 ui.Caption(journalButton,"TAB 查看日志",18,TextAnchor.MiddleCenter);
 ui.Click(journalButton,TidePause.RequestJournal);
 journalButton.GetComponent<Button>().navigation=new Navigation{mode=Navigation.Mode.None};
 journalButton.gameObject.SetActive(TideJournal.HasAny);
 bool resumed=TryResumeSavedLanding();
 if(number==1&&!resumed)ShowControls();
 }

 bool TryResumeSavedLanding(){
  string sceneName=gameObject.scene.name;
  bool requested=resumeLandingScene==sceneName;
  resumeLandingScene=null;
  string key=LandingKeyPrefix+sceneName;
  if(!requested){ClearSavedLanding(sceneName);return false;}
  string oldKey=OldLandingKeyPrefix+sceneName;
  bool legacy=!PlayerPrefs.HasKey(key);
  if(legacy&&!PlayerPrefs.HasKey(oldKey))return false;
  try{
   var saved=JsonUtility.FromJson<LandingCheckpoint>(PlayerPrefs.GetString(legacy?oldKey:key));
   if(saved!=null&&saved.version==(legacy?1:2)&&saved.visibleOrbs!=null&&saved.visibleOrbs.Length==orbs.Length&&
      saved.placedPlatforms!=null&&saved.flyingSpikes!=null&&saved.flyingSpikes.Length==flyingSpikes.StripCount&&
      saved.feet.x>=0&&saved.feet.x<=w&&saved.feet.y>=0&&saved.feet.y<h&&
      System.Array.TrueForAll(saved.placedPlatforms,p=>p.type>=0&&p.type<platformSprites.Length&&p.width>0)&&
      ResolveSavedCenter(saved,legacy)){
    checkpoint=saved;
    if(legacy){
     saved.version=2;
     PlayerPrefs.SetString(key,JsonUtility.ToJson(saved));
     PlayerPrefs.DeleteKey(oldKey);
     PlayerPrefs.Save();
    }
    if(number==10&&saved.movedExit){exitRelocated=true;exit.transform.position=RelocatedExitPosition;}
    RestoreLanding();
    return true;
   }
  }catch(System.ArgumentException e){Debug.LogWarning("无法读取关卡落脚点："+e.Message);}
  ClearSavedLanding(sceneName);
  return false;
 }

 void OnDestroy(){flyingSpikes?.Dispose();}
 // Inspect the original tile collision once, before hidden bricks or movable platforms are added.
 // Runs break at slopes, hazards and low ceilings. Their midpoints stay fixed for the whole room.
 void FindFixedLandings(){
  const int samplesPerTile=4;
  for(int y=1;y<h;y++){
   int start=-1;float height=0;
   for(int i=0;i<=w*samplesPerTile;i++){
    float x=(i+.5f)/samplesPerTile;
    bool clear=TrySolidHit(new Vector2(x,y+.18f),Vector2.down,.32f,out var hit)&&
      hit.collider is TilemapCollider2D&&hit.normal.y>.99f&&
      !TerrainHit(hit.point+Vector2.up*.035f,.025f,Body-.035f,out _);
    if(start>=0&&(!clear||Mathf.Abs(hit.point.y-height)>.025f)){
     float width=(i-start)/(float)samplesPerTile;
     if(width>=Half*2&&TryStandingCenter((i+start)*.5f/samplesPerTile,height,out var center))
      fixedLandings.Add(new FixedLanding{center=center,halfWidth=width*.5f,surfaceY=height});
     start=-1;
    }
    if(clear&&start<0){start=i;height=hit.point.y;}
   }
  }
 }
 bool TryStandingCenter(float x,float surfaceY,out Vector2 center){
  // At tile seams the ray is on the artwork while OverlapBox still sees its physics contact skin.
  // Keep the original centre and choose the first free height within the normal grounded tolerance.
  center=new Vector2(x,surfaceY+.002f);
  for(int step=0;step<13;step++){
   center.y=surfaceY+.002f+step*.004f;
   if(!Block(center,out _))return true;
  }
  return false;
 }
 int FixedLandingAt(Vector2 feet,bool fullSurface=false){
  for(int i=0;i<fixedLandings.Count;i++){
   var point=fixedLandings[i];
   float radius=fullSurface?point.halfWidth:Mathf.Min(.75f,point.halfWidth-.075f);
   if(Mathf.Abs(feet.y-point.surfaceY)<=.065f&&Mathf.Abs(feet.x-point.center.x)<=radius){
    if(TrySolidHit(feet+Vector2.up*.10f,Vector2.down,.17f,out var hit)&&hit.normal.y>.99f&&
       Mathf.Abs(hit.point.y-point.surfaceY)<.025f)return i;
   }
  }
  return -1;
 }
 bool ResolveSavedCenter(LandingCheckpoint saved,bool legacy){
  int i=FixedLandingAt(saved.feet,legacy);
  if(i<0)return false;
  Vector2 center=fixedLandings[i].center;
  if(!legacy&&Vector2.Distance(saved.feet,center)>.01f)return false;
  if(saved.startingPadEnabled&&startingPad&&OnPadTop(saved.feet,startingPad.transform.position,startingPad.bounds.extents.x))return false;
  foreach(var p in saved.placedPlatforms){
   if(OnPadTop(saved.feet,p.center,p.width*.5f))return false;
   if(Mathf.Abs(center.x-p.center.x)<p.width*.5f+Half&&center.y<p.center.y+.169f&&center.y+Body>p.center.y-.169f)return false;
  }
  saved.feet=center;return true;
 }
 static bool OnPadTop(Vector2 feet,Vector2 center,float halfWidth){
  return Mathf.Abs(feet.x-center.x)<halfWidth+Half&&Mathf.Abs(feet.y-center.y-.169f)<.065f;
 }
 void PrepareHiddenBrick(){
  if(number!=1 && number!=9)return;
  // Intercept the ordinary jump toward the stone balcony in room 1 or the upper exit steps in room 9.
  Vector2 center=number==1?new Vector2(7.5f,7.5f):new Vector2(25.5f,14.5f);
  foreach(var collider in terrain){
   if(!(collider is TilemapCollider2D tiles))continue;
   var map=tiles.GetComponent<Tilemap>();
   Vector3Int cell=map.WorldToCell(center);
   if(map.HasTile(cell))continue;
   Vector3Int floor=map.WorldToCell(new Vector2(spawn.x,spawn.y-.15f));
   TileBase tile=map.GetTile(floor);
   if(!tile||map.GetColliderType(floor)==Tile.ColliderType.None)continue;
   hiddenBrickMap=map;hiddenBrickCell=cell;hiddenBrickTile=tile;
   hiddenBrickBottom=center.y-.5f;hiddenBrickLeft=center.x-.5f;
   return;
  }
 }
 bool CheckHiddenBrick(Vector2 from,Vector2 to){
  if(!hiddenBrickMap||hiddenBrickShown||to.y<=from.y)return false;
  if(from.y+Body>hiddenBrickBottom+.001f||to.y+Body<hiddenBrickBottom)return false;
  if(to.x+Half<=hiddenBrickLeft||to.x-Half>=hiddenBrickLeft+1f)return false;
  hiddenBrickShown=true;
  SetHiddenBrick(hiddenBrickTile);
  return true;
 }
 void SetHiddenBrick(TileBase tile){
  hiddenBrickMap.SetTile(hiddenBrickCell,tile);
  hiddenBrickMap.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
  Physics2D.SyncTransforms();
 }
 void ShowControls(){
  welcome=ui.Panel("操作说明",.5f,.5f,0,0,730,390,TideHUD.ink);
  ui.Caption(ui.Box("标题",welcome,new Vector2(.5f,1),new Vector2(0,-14),new Vector2(600,42),Color.clear),"操作说明",25,TextAnchor.MiddleCenter);
  var body=ui.Box("平台说明",welcome,Vector2.one*.5f,new Vector2(0,8),new Vector2(680,242),Color.clear);
  ui.Caption(body,"A / D 或方向键移动，空格跳跃。\n拾取黄球后，按 Shift 进入放置视角，拖出一个平台。\n红台可落脚，绿台上按跳跃可高跳，紫台上按 V 可冲刺。\n对应的平台放到正确位置会变长，也可以自行放置、另找路线。\n放置视角中动作放慢，再按 Shift 返回。",20,TextAnchor.MiddleLeft);
  var close=ui.Box("开始关卡",welcome,new Vector2(.5f,0),new Vector2(0,17),new Vector2(155,44),new Color(.32f,.27f,.21f));
  ui.Caption(close,"开始关卡",20,TextAnchor.MiddleCenter);
  ui.Click(close,CloseControls);
 }
 void CloseControls(){if(!welcome)return;welcome.gameObject.SetActive(false);Destroy(welcome.gameObject);welcome=null;}
 void ToggleRouteHint(){
  if(!hintButton||!hintButton.gameObject.activeSelf||finished||TidePause.BlocksInput)return;
  hintActionFrame=Time.frameCount;
  viewingHint=!viewingHint;
  if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
  if(viewingHint){
   CancelDrag();admin=false;shiftView.CloseImmediately();
   foreach(var marker in hintViews)marker.enabled=false;
   if(!hintOverlay)BuildRouteHint();
   PositionRouteHint();
  }
  hintOverlay.gameObject.SetActive(viewingHint);
  hintButtonLabel.text=viewingHint?"H  收起提示图":"H  查看提示图";
 }
 void BuildRouteHint(){
  // Freeze the actual room and annotate its serialized placement targets. This keeps
  // the diagram aligned if a scene's terrain or recommended route is revised.
  hintOverlay=ui.Panel("本图放置示意",.5f,.5f,0,0,0,0,Color.clear);
  hintPlatforms=new RectTransform[placementHints.Length];
  for(int i=0;i<placementHints.Length;i++){
   var spot=placementHints[i];
   var marker=ui.Box("参考平台 "+(i+1),hintOverlay,Vector2.one*.5f,Vector2.zero,Vector2.one,Color.white,platformSprites[spot.kind]);
   marker.GetComponent<Image>().preserveAspect=false;
   hintPlatforms[i]=marker;
   var tag=ui.Box("放置顺序",marker,new Vector2(.5f,1),new Vector2(0,29),new Vector2(41,27),TideHUD.ink);
   ui.Caption(tag,(i+1).ToString(),17,TextAnchor.MiddleCenter);
  }
  string note=number==4?TideGuideBook.RoomFourNote:"按编号查看一种放置路线。平台的颜色表示类型，仍可自由尝试其他走法。";
  hintOverlay.anchorMin=Vector2.zero;hintOverlay.anchorMax=Vector2.one;
  hintOverlay.offsetMin=hintOverlay.offsetMax=Vector2.zero;
  var legend=ui.Box("路线说明",hintOverlay,new Vector2(.5f,0),new Vector2(0,8),new Vector2(720,37),TideHUD.ink);
  if(number==5)note="6 号红台下的刺会先抖动。趁它还没弹起，向右短跳，贴着倒刺下方落到门前。";
  if(number==9)note="在 3 号紫台朝右按 V，冲向 4 号绿台。落在绿台左端后，看准右侧巡逻再起跳。";
  ui.Caption(legend,note,17,TextAnchor.MiddleCenter);
 }
 void PositionRouteHint(){
  var camera=Camera.main;
  TideArt.CameraFrame(camera,w,h,w*.5f,h*.5f);
  float units=camera.pixelHeight/(camera.orthographicSize*2*ui.root.GetComponent<Canvas>().scaleFactor);
  for(int i=0;i<placementHints.Length;i++){
   var spot=placementHints[i];
   RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.root,camera.WorldToScreenPoint(spot.center),null,out var center);
   hintPlatforms[i].anchoredPosition=center;
   hintPlatforms[i].sizeDelta=new Vector2(spot.Width*units,.34f*units);
  }
 }
 void ReachExit(){
  if(finished)return;
  if(number==10&&!exitRelocated){
   exitRelocated=true;
   falseExitPosition=exit.transform.position;
   exitTravelLeft=ExitTravelTime;
   CancelDrag();
   admin=false;
   shiftView.CloseImmediately();
   if(dashSound)audioOut.PlayOneShot(dashSound);
   return;
  }
  finished=true;
  if(hintButton)hintButton.gameObject.SetActive(false);
  if(!string.IsNullOrEmpty(nextScene)&&nextScene.StartsWith("Trial",System.StringComparison.Ordinal)){SceneManager.LoadScene(nextScene);return;}
  openedExitFrame=Time.frameCount;
  string caption=string.IsNullOrEmpty(nextScene)?"回到灯塔小屋":"进入数据切片";
  storyExit=ui.Panel("出口交互",.5f,.15f,0,0,190,50,TideHUD.ink);
  ui.Caption(storyExit,caption,22,TextAnchor.MiddleCenter);ui.Click(storyExit,EnterStory);
 }
 void EnterStory(){
  if(!storyExit||openedExitFrame==Time.frameCount||TidePause.BlocksInput)return;
  storyExit.gameObject.SetActive(false);storyExit=null;
  SceneManager.LoadScene(string.IsNullOrEmpty(nextScene)?"01_Cottage":nextScene);
 }

 SpriteRenderer Make(string name,Vector2 center,int kind,float width=2){
  var r=new GameObject(name).AddComponent<SpriteRenderer>();
  r.transform.position=center;r.sortingOrder=5;SetPlatformArt(r,kind,width);
  return r;
 }
 void SetPlatformArt(SpriteRenderer renderer,int kind,float width){
  var sprite=platformSprites[kind];
  renderer.sprite=sprite;
  renderer.transform.localScale=new Vector3(width/sprite.bounds.size.x,.34f/sprite.bounds.size.y,1);
 }
 bool Overlap(Vector2 p,float hw,float ht){return TerrainHit(p,hw,ht,out _);}
 bool TerrainHit(Vector2 p,float hw,float ht,out bool spike){
  spike=false;
  bool solid=false;
  int count=Physics2D.OverlapBoxNonAlloc(p+Vector2.up*(ht*.5f),new Vector2(hw*2-.002f,ht-.002f),0,hits);
  for(int i=0;i<count;i++){
   if(!terrain.Contains(hits[i]))continue;
   solid=true;
   if(spikes.Contains(hits[i]))spike=true;
  }
  return solid;
 }
 const int Spike=-2;
 bool Block(Vector2 p,out int k){k=-1;
 if(TerrainHit(p,Half,Body,out bool spike)){
  if(spike){k=Spike;return true;}
  // On a ramp the soles meet the surface at their centre. Keep the shoulders and head full-width.
  const float ankleHeight=Half+.045f;
  if(TerrainHit(p+Vector2.up*ankleHeight,Half,Body-ankleHeight,out _))return true;
  if(TryRampFloor(p,out float floorY)){
   if(p.y<floorY-.001f)return true;
  }else{
   // OverlapBox also reports touching faces. Let a foot at the exact ramp/flat seam move sideways.
   const float soleSkin=.025f;
   if(TerrainHit(p+Vector2.up*soleSkin,Half,Body-soleSkin,out _)||
      !TrySolidHit(p+Vector2.up*soleSkin,Vector2.down,soleSkin+.002f,out var flat)||
      flat.normal.y<.99f||p.y<flat.point.y-.001f)return true;
  }
 }
 if(p.x<Half||p.x>w-Half||p.y+Body>h)return true;
 foreach(var pad in pads)if(Mathf.Abs(p.x-pad.center.x)<pad.halfWidth+Half-.001f&&p.y<pad.center.y+.169f&&p.y+Body>pad.center.y-.169f){k=pad.type;return true;}return false;}
 // Test only the strip immediately below the soles. A wall beside the torso is not a floor.
 bool FeetSupported(float depth,out int kind){
  kind=-1;
  // Leave a narrow margin outside the soles so a tangent wall cannot support a fall.
  if(TerrainHit(pos-Vector2.up*depth,Half-.025f,depth,out bool spike)){
   if(spike){kind=Spike;return true;}
   if(!TryRampFloor(pos,out float floorY)||Mathf.Abs(pos.y-floorY)<=depth+.001f)return true;
  }
  foreach(var pad in pads){
   float gap=pos.y-(pad.center.y+.169f);
   if(gap>=-.002f&&gap<=depth&&Mathf.Abs(pos.x-pad.center.x)<pad.halfWidth+Half-.001f){
    kind=pad.type;return true;
   }
  }
  return false;
 }
 bool FindSoleSlope(float depth,out Vector2 normal){
  normal=Vector2.zero;
  if(TryRampFloor(pos,out _))return false;
  // The hero slides when only the outer edge of the collision box touches a steep slope.
  if(TerrainHit(pos-Vector2.up*depth,.12f,depth,out _))return false;
  for(int side=-1;side<=1;side+=2){
   Vector2 toe=pos+new Vector2(side*(Half-.002f),.02f);
   if(!SolidSurface(toe,Vector2.down,depth+.053f,out Vector2 floor))continue;
   // Some diagonal tiles have a short flat lip where they meet a vertical wall.
   if(floor.y>.9f){
    if(!SolidSurface(pos+Vector2.up*.65f,Vector2.right*side,Half+.15f,out Vector2 wall)||wall.x*side>-.94f)continue;
    if(!SolidSurface(toe-Vector2.right*(side*.15f),Vector2.down,depth+.20f,out floor))continue;
   }
   if(floor.x*side>=-.35f||floor.y<.45f||floor.y>.9f)continue;
   normal=floor;
   return true;
  }
  return false;
 }
 bool SolidSurface(Vector2 origin,Vector2 direction,float distance,out Vector2 normal){
  bool found=TrySolidHit(origin,direction,distance,out var surface);
  normal=surface.normal;
  return found;
 }
 bool TrySolidHit(Vector2 origin,Vector2 direction,float distance,out RaycastHit2D surface){
  surface=default;
  int count=Physics2D.RaycastNonAlloc(origin,direction,surfaceHits,distance);
  float nearest=float.PositiveInfinity;
  for(int i=0;i<count;i++){
   var hit=surfaceHits[i];
   if(hit.distance<=0||hit.distance>=nearest||!terrain.Contains(hit.collider)||spikes.Contains(hit.collider))continue;
   nearest=hit.distance;
   surface=hit;
  }
  return nearest<float.PositiveInfinity;
 }
 // Probe the actual tile contour, including the flat ground at either end of the ramp.
 bool TryRampFloor(Vector2 feet,out float floorY){
  const float reach=Half+.12f;
  floorY=0;
  Vector2 from=feet+Vector2.up*reach;
  if(!TrySolidHit(from,Vector2.down,reach*2,out var middle)||middle.normal.y<.45f)return false;
  floorY=middle.point.y;
  if(IsWalkableRamp(middle))return true;
  if(middle.normal.y<.9f)return false;
  for(int side=-1;side<=1;side+=2){
   if(TrySolidHit(from+Vector2.right*(side*(Half+.025f)),Vector2.down,reach*2,out var toe)&&IsWalkableRamp(toe))return true;
  }
  return false;
 }
 bool IsWalkableRamp(RaycastHit2D surface){
  if(surface.normal.y<.45f||surface.normal.y>.90f||Mathf.Abs(surface.normal.x)<.35f)return false;
  var map=surface.collider.GetComponent<Tilemap>();
  if(!map)return false;
  // A dangling bevel under a vertical wall is not a landing. Allow connected stairs to meet walls.
  var cell=map.WorldToCell(surface.point-surface.normal*.015f);
  Vector2 center=map.GetCellCenterWorld(cell);
  float uphill=-Mathf.Sign(surface.normal.x);
  Vector2 aboveLip=center+new Vector2(uphill*.55f,.79f);
  Vector2 belowToe=center-new Vector2(uphill*.55f,.60f);
  return !TerrainHit(aboveLip,.018f,.04f,out _)||TerrainHit(belowToe,.018f,.04f,out _);
 }
 bool SlideOffSoleSlope(Vector2 next,float depth){
  if(!FindSoleSlope(depth,out Vector2 slope))return false;
  var sideways=Vector2.right*(Mathf.Max(.025f,depth*slope.y/Mathf.Abs(slope.x)+.004f)*Mathf.Sign(slope.x));
  Vector2 slide=next+sideways;
  if(Block(slide,out _)){
   slide=pos+sideways;
   if(Block(slide,out _))return false;
  }
  pos=slide;
  if(horizontalSpeed*slope.x<0)horizontalSpeed=0;
  return true;
 }
 void RestoreStartingPad(){
  padTime=-1;
  if(!startingPad)return;
  startingPad.color=Color.white;
  startingPad.enabled=true;
  pads.Add(new Pad{center=startingPad.transform.position,halfWidth=startingPad.bounds.extents.x,type=0,view=startingPad});
 }
 void ClearPlatforms(){
  foreach(var pad in pads){
   if(!pad.view||pad.view==startingPad)continue;
   pad.view.enabled=false;
   Destroy(pad.view.gameObject);
  }
  pads.Clear();
 }
 void AddPlacedPlatform(Vector2 center,int kind,float width){
  var view=Make("已放置平台",center,kind,width);
  pads.Add(new Pad{center=center,halfWidth=width*.5f,type=kind,view=view});
 }
 bool[] CaptureOrbs(){
  var visible=new bool[orbs.Length];
  for(int i=0;i<orbs.Length;i++)visible[i]=orbs[i].gameObject.activeSelf;
  return visible;
 }
 void RestoreOrbs(bool[] visible){
  for(int i=0;i<orbs.Length;i++)orbs[i].gameObject.SetActive(visible[i]);
 }
 void ResetRoom(){
  ClearSavedLanding(gameObject.scene.name);
  if(hiddenBrickShown){SetHiddenBrick(null);hiddenBrickShown=false;}
  flyingSpikes.Reset();
  patrols?.Reset();
  if(decoy){decoy.enabled=false;decoyArmed=decoyTriggered=false;decoyAge=0;}
  ClearPlatforms();
  RestoreStartingPad();
  RestoreOrbs(initialOrbs);
  charge=0;cooldown=0;checkpoint=null;
  ResetPlayer(spawn,spawnFacingRight);
 }
 void ResetPlayer(Vector2 feet,bool facingRight){
  feedback?.Reset();
  CancelDrag();
  foreach(var hint in hintViews)hint.enabled=false;
  finished=admin=airborne=playerJump=jumpReleased=fastFallHeld=false;
  jumpBuffer=ledgeGrace=horizontalSpeed=vy=dash=dxDash=clock=0;
  landingAge=landingDuration=0;respawnAt=0;
  takeoffAge=float.PositiveInfinity;
  support=-1;pos=feet;
  touchedLanding=checkpoint==null?-1:FixedLandingAt(feet);
  audioOut.Stop();
  hero.flipX=facingRight;hero.color=Color.white;
  energyFill.sizeDelta=new Vector2(72*charge,5);
  shiftView.CloseImmediately();
  RenderHero(false);
  if(hintButton)hintButton.gameObject.SetActive(TideGuideBook.Unlocked(number));
 }
 // Capture after a landing and after pickups, so the orb, energy and pad state travel together.
 void RecordLanding(){
  const float reactionTime=.30f;
  if(airborne){
   // A sub-step can settle the last millimetres after respawn without leaving the platform.
   if(touchedLanding>=0&&(vy>0||FixedLandingAt(pos)!=touchedLanding))touchedLanding=-1;
   return;
  }
  int i=FixedLandingAt(pos);
  if(i<0){touchedLanding=-1;return;}
  if(i==touchedLanding)return;
  foreach(var pad in pads)
   if(OnPadTop(pos,pad.center,pad.halfWidth))return;
  var center=fixedLandings[i].center;
  if(Block(center,out _)||flyingSpikes.ThreatensLanding(center,Half,Body,reactionTime)||
     (patrols!=null&&patrols.ThreatensLanding(center,Half,Body,reactionTime)))return;
  if(decoy&&decoy.enabled&&decoyTriggered){
   var area=decoy.bounds;
   area.Expand(new Vector3(2.6f,1.5f,0));
   if(area.Intersects(new Bounds(center+Vector2.up*(Body*.5f),new Vector3(Half*2,Body,0))))return;
  }
  var placed=new List<PlacedPlatformState>(pads.Count);
  foreach(var pad in pads)if(pad.view!=startingPad)
   placed.Add(new PlacedPlatformState{center=pad.center,type=pad.type,width=pad.halfWidth*2});
  checkpoint=new LandingCheckpoint{
   version=2,movedExit=exitRelocated,
   feet=center,facingRight=hero.flipX,hiddenBrickShown=hiddenBrickShown,placedPlatforms=placed.ToArray(),
   visibleOrbs=CaptureOrbs(),charge=charge,cooldown=cooldown,padTime=padTime,
   startingPadEnabled=startingPad&&startingPad.enabled,startingPadColor=startingPad?startingPad.color:Color.white,
   flyingSpikes=flyingSpikes.CaptureRetryState(),patrolDistance=patrols?.Travelled??0,
   decoyEnabled=decoy&&decoy.enabled,decoyArmed=decoyArmed,decoyTriggered=decoyTriggered,
   decoyAge=decoyAge,decoyPad=decoyPad,decoyPosition=decoy?decoy.transform.position:Vector3.zero,
   decoyScale=decoy?decoy.transform.localScale:Vector3.one,decoyColor=decoy?decoy.color:Color.white
  };
  touchedLanding=i;
  PlayerPrefs.SetString(LandingKeyPrefix+gameObject.scene.name,JsonUtility.ToJson(checkpoint));
  PlayerPrefs.Save();
 }
 void RestoreLanding(){
  var saved=checkpoint;
  if(saved==null){ResetRoom();return;}
  if(hiddenBrickShown!=saved.hiddenBrickShown){
   hiddenBrickShown=saved.hiddenBrickShown;
   SetHiddenBrick(hiddenBrickShown?hiddenBrickTile:null);
  }
  ClearPlatforms();
  if(startingPad){
   if(saved.startingPadEnabled)RestoreStartingPad();
   else startingPad.enabled=false;
   startingPad.color=saved.startingPadColor;
  }
  foreach(var platform in saved.placedPlatforms)AddPlacedPlatform(platform.center,platform.type,platform.width);
  padTime=saved.padTime;
  RestoreOrbs(saved.visibleOrbs);
  charge=saved.charge;cooldown=saved.cooldown;
  flyingSpikes.RestoreRetryState(saved.flyingSpikes);
  patrols?.RestoreRetryDistance(saved.patrolDistance);
  decoyArmed=saved.decoyArmed;decoyTriggered=saved.decoyTriggered;decoyAge=saved.decoyAge;decoyPad=saved.decoyPad;
  if(decoyArmed)EnsureDecoy();
  if(decoy){
   decoy.enabled=saved.decoyEnabled;
   decoy.transform.position=saved.decoyPosition;
   decoy.transform.localScale=saved.decoyScale;
   decoy.color=saved.decoyColor;
   decoy.sprite=decoyTriggered?errorIcon:TideArt.Pic("Art/Orb");
  }
  ResetPlayer(saved.feet,saved.facingRight);
 }
 const float HitPause=.065f,HitFade=.095f;
 void BeginRetry(bool outOfView){
  if(finished)return;
  CollectTouchedJournal();
  failures++;
  if(hintButton && failures>=Mathf.Max(1,failuresBeforeHint))TideGuideBook.Unlock(number);
  if(hintButton)hintButton.gameObject.SetActive(false);
  finished=true;
  respawnAt=Time.timeAsDouble+(outOfView?0:HitPause+HitFade);
  CancelDrag();
  foreach(var hint in hintViews)hint.enabled=false;
  RenderHero(false);
  hero.color=new Color(1f,.52f,.5f);
  footShadow.enabled=false;
 }
 void TickStartingPad(float dt){
  if(startingPadLifetime<=0||!startingPad||!startingPad.enabled)return;
  Vector2 center=startingPad.transform.position;
  bool standing=vy<=0&&Mathf.Abs(pos.x-center.x)<startingPad.bounds.extents.x+Half&&Mathf.Abs(pos.y-center.y-.169f)<=.065f;
  if(number==2&&!standing){
   padTime=-1;
   startingPad.color=Color.white;
   return;
  }
  if(padTime<0){
   if(!standing)return;
   padTime=0;
   return;
  }
  padTime+=dt;
  float remaining=startingPadLifetime-padTime;
  if(remaining<=0){
   pads.RemoveAll(p=>p.view==startingPad);
   startingPad.enabled=false;
  }else if(remaining<1.5f){
   startingPad.color=new Color(1,1,1,.35f+.65f*Mathf.Abs(Mathf.Sin(remaining*10)));
  }
 }
 void Update(){
 if(TidePause.BlocksInput)return;
 if(exitTravelLeft>0){
  exitTravelLeft=Mathf.Max(0,exitTravelLeft-Time.deltaTime);
  float t=Mathf.SmoothStep(0,1,1-exitTravelLeft/ExitTravelTime);
  exit.transform.position=Vector3.Lerp(falseExitPosition,RelocatedExitPosition,t)+Vector3.up*(4*t*(1-t));
  if(exitTravelLeft==0)ResetRoom();
  return;
 }
 if(OpenJournalWhenGrounded())return;
 if(hintActionFrame==Time.frameCount)return;
 if(hintButton&&hintButton.gameObject.activeSelf&&Input.GetKeyDown(KeyCode.H)){ToggleRouteHint();return;}
 if(viewingHint){PositionRouteHint();return;}
 if(admin && ((hintButton&&hintButton.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(hintButton,Input.mousePosition))
    || (journalButton.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(journalButton,Input.mousePosition)))
    &&(Input.GetMouseButtonDown(0)||Input.GetMouseButtonUp(0))){CancelDrag();return;}
 if(welcome){if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.E))CloseControls();return;}
 if(storyExit){
  if(Input.GetMouseButtonDown(0)){
   Vector3 mouse=Camera.main.ScreenToWorldPoint(Input.mousePosition);mouse.z=exit.bounds.center.z;
   if(exit.bounds.Contains(mouse))EnterStory();
  }
  return;
 }
 if(respawnAt>0){
  float remaining=(float)(respawnAt-Time.timeAsDouble);
  if(Input.GetKeyDown(KeyCode.R)){ResetRoom();return;}
  if(remaining<=0){RestoreLanding();return;}
  var tint=hero.color;
  tint.a=Mathf.Clamp01(remaining/HitFade);
  hero.color=tint;
  return;
 }
 if(finished)return;
 cooldown=Mathf.Max(0,cooldown-Time.unscaledDeltaTime);
 TickStartingPad(Time.unscaledDeltaTime);
 if(Input.GetKeyDown(KeyCode.R)){ResetRoom();return;}
 if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift)){admin=!admin;CancelDrag();}
 shiftView.Tick(admin,charge,cooldown);
 if(admin){
  shiftView.ShowInstruction(charge==0?string.Empty:cooldown>0?"平台冷却中":"点选或拖出平台，在地图内放置");
  if(Input.GetMouseButtonDown(1)||Input.GetKeyDown(KeyCode.Escape))CancelDrag();
  else Manage(Input.mousePosition,Input.GetMouseButtonDown(0),Input.GetMouseButtonUp(0));
 }
 for(int i=0;i<hintViews.Count;i++)hintViews[i].enabled=admin&&drag==placementHints[i].kind&&PlacementError(placementHints[i].center,placementHints[i].Width)==null;
 float dt=Mathf.Min(Time.deltaTime,.05f)*shiftView.GameSpeed;
 bool jump=Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W);
 bool held=Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W);
 fastFallHeld=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow);
 Move(dt,Input.GetAxisRaw("Horizontal"),jump,held,Input.GetKeyDown(KeyCode.V),Time.unscaledDeltaTime);
 energyFill.sizeDelta=new Vector2(Mathf.MoveTowards(energyFill.sizeDelta.x,72*charge,720*Time.unscaledDeltaTime),5);
 }
 void Move(float dt,float dir,bool jump,bool held,bool sprintPressed,float inputDt){
 // Keep the jump arc consistent between Game view frame rates and in Shift slow motion.
  float startX=pos.x;
  int slices=Mathf.Max(1,Mathf.CeilToInt(dt*120f));
  for(int i=0;i<slices&&!finished&&exitTravelLeft<=0&&!TidePause.JournalOpen;i++)
   StepMovement(dt/slices,dir,i==0&&jump,held,i==0&&sprintPressed,inputDt/slices);
  if(finished)return;
  float travel=pos.x-startX;
  float facing=dash>0?dxDash:dir!=0?dir:travel;
  if(facing!=0)hero.flipX=facing>0;
  RenderHero(Mathf.Abs(travel)>dt*.15f);
  feedback?.Tick(dt,dash>0);
 }
 void StepMovement(float dt,float dir,bool jump,bool held,bool sprintPressed,float inputDt){
 Vector2 previousFeet=pos;
 clock+=dt;takeoffAge+=dt;landingAge+=dt;
 float groundDepth=spikeHazards?.06f:.035f;
 bool onGround=FeetSupported(groundDepth,out support)&&(support!=-1||!FindSoleSlope(groundDepth,out _));
 if(jump&&onGround&&!airborne&&vy==0&&support!=Spike)RecordLanding();
 jumpBuffer=jump?.13f:Mathf.Max(0,jumpBuffer-inputDt);
 ledgeGrace=onGround&&vy<=0&&support!=Spike?.10f:Mathf.Max(0,ledgeGrace-dt);
 if(onGround&&support==1&&vy<=0){
  if(jump)Bounce();
 }
 else if(ledgeGrace>0&&jumpBuffer>0){
  vy=JumpSpeed;playerJump=true;jumpReleased=false;takeoffAge=0;landingDuration=0;
  ledgeGrace=jumpBuffer=0;
 }
 if(onGround&&support==2&&vy<=0&&sprintPressed&&dash<=0){
  dxDash=dir!=0?Mathf.Sign(dir):(hero.flipX?1f:-1f);
  dash=DashDuration();playerJump=false;landingDuration=0;
  if(dashSound)audioOut.PlayOneShot(dashSound);
 }
 bool boost=dash>0;
 float boostedTime=boost?Mathf.Min(dash,dt):0;
 if(boost)vy=0;
 if(!held)jumpReleased=true;
 float verticalTime=dt-boostedTime;
 if(vy<0){
  bool fast=fastFallHeld&&!admin;
  float gravity=Mathf.Lerp(24f,33f,Mathf.InverseLerp(0,12f,-vy));
  if(playerJump&&!jumpReleased&&vy> -2.3f)gravity=RiseGravity*.58f;
  vy=Mathf.MoveTowards(vy,fast?-FastFallLimit:-FallLimit,(fast?50f:gravity)*verticalTime);
 }else{
  float gravity=playerJump?RiseGravity:22f;
  if(playerJump)gravity*=jumpReleased?2.6f:vy<2.3f?.58f:1f;
  vy-=gravity*verticalTime;
 }
 dash=Mathf.Max(0,dash-dt);
 if(boost)horizontalSpeed=dxDash*RunSpeed;
 float travel=dxDash*DashSpeed*boostedTime+RunTravel(dir,dt-boostedTime,onGround);
 float vx=dt>0?travel/dt:0;
 int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(vx),Mathf.Abs(vy))*dt/.06f));
 for(int i=0;i<steps;i++){
  Vector2 nx=pos+Vector2.right*(vx*dt/steps);
  // Follow both uphill and downhill by the distance actually travelled this substep.
  if(vx!=0&&onGround&&support==-1&&vy<=0&&!boost&&TryRampFloor(nx,out float floorY)&&
     Mathf.Abs(nx.y-floorY)<=Mathf.Abs(nx.x-pos.x)*1.6f+.012f)nx.y=floorY;
  if(!Block(nx,out int side))pos=nx;
  else if(side==Spike){BeginRetry(false);return;}
  else if(MarkedRoute&&side==-1&&onGround&&vy<=0&&vx!=0&&!Block(nx+Vector2.up*.06f,out _))pos=nx+Vector2.up*.06f;
  else if(vx!=0){horizontalSpeed=0;vx=0;}
  Vector2 ny=pos+Vector2.up*(vy*dt/steps);
  if(CheckHiddenBrick(pos,ny)){
   pos.y=hiddenBrickBottom-Body-.002f;vy=0;playerJump=false;jumpReleased=true;
   continue;
  }
  bool verticalHit=Block(ny,out int tile);
  if(ny.y<pos.y&&tile==-1&&TryRampFloor(pos,out float landingY)&&landingY<=pos.y+.001f&&landingY>=ny.y-.001f){
   Vector2 landing=new Vector2(pos.x,landingY);
   if(!Block(landing,out _)){pos=landing;verticalHit=true;}
  }
  if(verticalHit&&tile==-1&&ny.y<pos.y&&SlideOffSoleSlope(ny,pos.y-ny.y)){vx=0;continue;}
  // On descent the feet lead. A shoulder still touching an overhang must be able to slide free.
  if(verticalHit && tile!=Spike && ny.y<pos.y)
   verticalHit=FeetSupported(Mathf.Max(.003f,pos.y-ny.y),out tile);
  if(!verticalHit)pos=ny;
  else if(tile==Spike){BeginRetry(false);return;}
  else if(!(MarkedRoute&&vy>0&&tile==-1&&CorrectBrickCorner(ny,dir))){
   if(vy < -7)audioOut.PlayOneShot(land,.16f);
   if(vy< -4f){
    landingDuration=Mathf.Lerp(.09f,.16f,Mathf.InverseLerp(4,22,-vy));
    landingAge=0;
   }
   vy=0;playerJump=false;takeoffAge=float.PositiveInfinity;
  }
 }
 if(pos.y< -2){BeginRetry(true);return;}
 if(flyingSpikes.Tick(previousFeet,pos,Half,Body,dt)
    || (patrols!=null && patrols.Tick(dt,previousFeet,pos,Half,Body))
    || TickDecoy(dt,previousFeet,pos)){
  BeginRetry(false);
  return;
 }
 airborne=vy!=0||!FeetSupported(.06f,out _);
 foreach(var orb in orbs)if(charge==0&&orb.gameObject.activeSelf&&Vector2.Distance(pos+Vector2.up*.65f,orb.transform.position)<1.05f){charge=1;orb.gameObject.SetActive(false);audioOut.PlayOneShot(pickup);}
 RecordLanding();
 CollectTouchedJournal();
 if(OpenJournalWhenGrounded())return;
 if(Vector2.Distance(pos+Vector2.up*.7f,exit.transform.position)<1.1f)ReachExit();
 }
 void CollectTouchedJournal(){
  if(fragment==null||!fragment.Touch(pos,Half,Body))return;
  journalButton.gameObject.SetActive(true);
  pendingJournal=fragmentDocument;
  journalRequested=true;
 }
 internal void RequestJournalList(){
  journalRequested=true;
  OpenJournalWhenGrounded();
 }
 bool OpenJournalWhenGrounded(){
  if(!journalRequested || respawnAt>0 || airborne || vy!=0 || !FeetSupported(.06f,out int floor) || floor==Spike)return false;
  if(!TidePause.ShowJournal(pendingJournal))return false;
  pendingJournal=null;journalRequested=false;
  CancelDrag();
  if(admin){admin=false;shiftView.CloseImmediately();}
  return true;
 }
 // Trial08: the first marked bounce pad seems to offer a bonus pickup above it.
 // Landing primes the false pickup once. Its sweep stays above a standing player.
 void ArmDecoy(Vector2 center,int kind){
  if(number!=8||kind!=1||!MarkedRoute||center!=placementHints[0].center||decoyArmed)return;
  EnsureDecoy();
  decoyPad=center;decoyArmed=true;decoy.enabled=true;
  TideArt.Fit(decoy,TideArt.Pic("Art/Orb"),.62f);
  decoy.color=new Color(.82f,.96f,1f);
  decoy.transform.position=center+Vector2.up*3.2f;
 }
 void EnsureDecoy(){
  if(decoy)return;
  decoy=new GameObject("异常能量球").AddComponent<SpriteRenderer>();
  decoy.transform.SetParent(transform,false);decoy.sortingOrder=8;
  errorIcon=TideArt.Pic("Art/DataErrorPatrol");
 }
 bool TickDecoy(float dt,Vector2 previous,Vector2 current){
  if(!decoyArmed||!decoy.enabled)return false;
  if(!decoyTriggered){
   float top=decoyPad.y+.169f;
   bool stepped=Mathf.Min(Mathf.Abs(current.y-top),Mathf.Abs(previous.y-top))<=.065f;
   if(!stepped||Mathf.Abs(current.x-decoyPad.x)>.78f)return false;
   decoyTriggered=true;
   TideArt.Fit(decoy,errorIcon,.69f);
   decoy.color=Color.white;
  }
  decoyAge+=dt;
  if(decoyAge>=.57f){decoy.enabled=false;return false;}
  // The distortion hangs in the jump lane first, then slips sideways and fades.
  float away=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.19f,.54f,decoyAge));
  decoy.transform.position=decoyPad+new Vector2(2.6f*away,3.2f-.75f*Mathf.Clamp01(decoyAge/.19f));
  decoy.color=new Color(1,1,1,1-Mathf.InverseLerp(.38f,.57f,decoyAge));
  if(decoyAge>=.45f)return false;
  Bounds area=decoy.bounds;Vector2 point=area.center;
  return Mathf.Abs(current.x-point.x)<Half+area.extents.x*.85f &&
    current.y<point.y+area.extents.y*.85f && current.y+Body>point.y-area.extents.y*.85f;
 }
 float DashDuration(){
  if(number==9 && dxDash>0 && placementHints!=null && placementHints.Length>=4){
   var launch=placementHints[2];var landing=placementHints[3];
   if(launch.kind==2 && landing.kind==1 && Mathf.Abs(pos.y-launch.center.y-.169f)<.075f &&
      Mathf.Abs(pos.x-launch.center.x)<launch.Width*.5f+Half){
    // The long gap is crossed from anywhere on the marked purple pad. Aim within the
    // receiving pad's left edge, leaving the right-hand patrol visible and avoidable.
    float target=landing.center.x-landing.Width*.5f+Half;
    return (target-pos.x)/DashSpeed;
   }
  }
  return number<=8&&MarkedRoute?.48f:.30f;
 }
 const float DashSpeed=24f;
 internal const float RunSpeed=5.7f;
 float RunTravel(float input,float dt,bool grounded){
  float target=input*RunSpeed;
  if(input==0)return AccelerateToward(0,grounded?100:76,dt);
  float distance=0;
  if(horizontalSpeed*target<0){
   float turnBrake=grounded?130:125;
   float brakeTime=Mathf.Min(dt,Mathf.Abs(horizontalSpeed)/turnBrake);
   distance=AccelerateToward(0,turnBrake,brakeTime);
   dt-=brakeTime;
  }
  float acceleration=grounded?68f:Mathf.Abs(vy)<3f?72f:58f;
  return distance+AccelerateToward(target,acceleration,dt);
 }
 float AccelerateToward(float target,float rate,float dt){
  float before=horizontalSpeed;
  float rampTime=Mathf.Min(dt,Mathf.Abs(target-before)/rate);
  horizontalSpeed=Mathf.MoveTowards(before,target,rate*dt);
  return (before+horizontalSpeed)*.5f*rampTime+horizontalSpeed*(dt-rampTime);
 }
 // The tallest marked climb from a green pad is just over 8 tiles.
 float BounceSpeed=>Mathf.Sqrt(2f*22f*(MarkedRoute?11f:5.1f));
 void Bounce(){
  vy=BounceSpeed;
  playerJump=false;takeoffAge=.08f;landingDuration=0;
  ledgeGrace=jumpBuffer=0;
  if(feedback==null)return;
  foreach(var pad in pads)if(pad.type==1&&Mathf.Abs(pos.x-pad.center.x)<pad.halfWidth+Half&&Mathf.Abs(pos.y-pad.center.y-.169f)<.08f){
   feedback.Bounce(pad.view);
   break;
  }
 }
 bool CorrectBrickCorner(Vector2 next,float direction){
  // Slide only across a clear horizontal path. Any spike along the sweep rules that side out.
  if(next.y+Body>h)return false;
  float preferred=direction<0?-1:1;
  for(int side=0;side<2;side++){
   float sign=side==0?preferred:-preferred;
   for(int i=1;i<=3;i++){
    var offset=Vector2.right*(sign*.04f*i);
    if(Block(pos+offset,out _))break;
    if(!Block(next+offset,out int obstacle)){pos=next+offset;return true;}
    if(obstacle!=-1)break;
   }
  }
  return false;
 }
 // The side idle frames have extra transparent space below the boots.
 public const float IdleCenterHeight=.62f;
 void RenderHero(bool moving){
  footShadow.enabled=!airborne;
  float center=.67f;
  if(airborne)hero.sprite=jumpFrames[AirFrame()];
  else if(landingAge<landingDuration){
   float progress=landingAge/landingDuration;
   int frame=progress<.18f?6:progress<.52f?7:progress<.84f?8:9;
   hero.sprite=jumpFrames[frame];
  }else{
   var frames=moving?walking:idle;
   hero.sprite=frames[(int)(clock*(moving?10:6))%frames.Length];
   if(!moving)center=IdleCenterHeight;
  }
  hero.transform.position=pos+Vector2.up*center;
 }
 int AirFrame(){
  if(dash>0)return 3;
  if(takeoffAge<.03f)return 1;
  if(takeoffAge<.065f)return 2;
  if(vy>7f)return 3;
  if(vy>1.5f)return 4;
  if(vy> -7f)return 5;
  // The reaching boot also reads as the extension before touchdown.
  return 3;
 }
 void Manage(Vector2 mouse,bool pressed,bool released){
 int button=shiftView.Pick(mouse,true);
 if(pressed&&button>=0&&charge>0&&cooldown<=0)drag=button;
 if(drag<0)return;
 if(!shiftView.Ready){if(released&&button<0)CancelDrag();else shiftView.Drag(drag,mouse,false);return;}
 bool inLevel=shiftView.InLevel(mouse);shiftView.Drag(drag,mouse,inLevel);
 Vector2 m=PlacementCenter(Camera.main.ScreenToWorldPoint(mouse),drag);
 float width=PlatformWidth(m,drag);
 string reason=inLevel?PlacementError(m,width):"";
 bool valid=inLevel&&reason==null;
 string usage=drag==0?"普通平台可落脚":drag==1?"站上绿台后按跳跃键高跳":"站上紫台后按 V 冲刺";
 shiftView.ShowInstruction(inLevel&&!valid?reason:inLevel&&width>2f?"位置匹配，平台伸长　右键取消":usage+"　右键取消");
 ghost.enabled=inLevel;ghost.transform.position=m;
 SetPlatformArt(ghost,drag,width);
 ghost.color=valid?new Color(1,1,1,.65f):new Color(.45f,.45f,.45f,.45f);
 if(released){
  if(valid)PlacePlatform(m,drag);
  if(valid||(!inLevel&&button<0))CancelDrag();
 }
 }
 Vector2 PlacementCenter(Vector2 mouseWorld,int kind){
  if(MarkedRoute&&placementHints!=null)foreach(var spot in placementHints)
   if(spot.kind==kind&&(spot.center-mouseWorld).sqrMagnitude<.55f*.55f)return spot.center;
  return new Vector2(Mathf.Round(mouseWorld.x*2)/2,Mathf.Round(mouseWorld.y*2)/2);
 }
 void CancelDrag(){drag=-1;ghost.enabled=false;shiftView.Drag(-1,Vector2.zero,false);}
 void OnApplicationFocus(bool focused){if(!focused&&drag>=0)CancelDrag();}
 float PlatformWidth(Vector2 m,int kind){
  if(placementHints!=null)foreach(var spot in placementHints)if(spot.kind==kind&&spot.center==m)return spot.Width;
  return 2;
 }
 string PlacementError(Vector2 m,float width){
 float halfWidth=width*.5f;
 if(charge<=0)return "先拾取一颗黄球";
 if(cooldown>0)return "平台冷却中";
 if(m.x<halfWidth||m.x>w-halfWidth||m.y<.2f||m.y>=h-.2f)return "平台超出了地图边缘";
 if(Overlap(m-Vector2.up*.17f,halfWidth,.34f))return "这里有地形，请移到空处";
 if(Mathf.Abs(pos.x-m.x)<halfWidth+Half&&pos.y<m.y+.17f&&pos.y+Body>m.y-.17f)return "平台和角色重叠了";
 foreach(var p in pads)if(Mathf.Abs(p.center.x-m.x)<halfWidth+p.halfWidth&&Mathf.Abs(p.center.y-m.y)<.34f)return "这里已有平台";
 return null;
 }
 bool PlacePlatform(Vector2 m,int kind){
 float width=PlatformWidth(m,kind);
 if(kind<0||kind>=platformSprites.Length||PlacementError(m,width)!=null)return false;
  AddPlacedPlatform(m,kind,width);charge=0;cooldown=.65f;ArmDecoy(m,kind);return true;
 }
}
