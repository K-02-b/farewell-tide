using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class TideImporter:AssetPostprocessor{
 public static void Setup(TextureImporter i){i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=i.assetPath.StartsWith("Assets/Tide/Resources/Tide/Anim/ManJump/",StringComparison.Ordinal)?174.4f:100f;i.mipmapEnabled=false;i.alphaIsTransparency=true;i.npotScale=TextureImporterNPOTScale.None;i.filterMode=i.assetPath.Contains("/Maps/")?FilterMode.Point:FilterMode.Bilinear;i.textureCompression=TextureImporterCompression.Uncompressed;if(i.assetPath.Contains("/UI/"))i.spriteBorder=new Vector4(8,8,8,8);}
 void OnPreprocessTexture(){if(assetPath.StartsWith("Assets/Tide/Resources/Tide/"))Setup((TextureImporter)assetImporter);}
}
public static class TideScenes {
 static readonly string[] acts={"01_Cottage","02_Lighthouse","03_Meadow","04_Rift"};
 static readonly string[] memories={"05_MemoryCottage","06_MemoryShore","07_LanternDeck"};
 static readonly string[] maps={"0","01","03","04","05","2","3","4","5","6"};
 const string streetScene="12_StreetOrigin";
 const string parkScene="11_ParkPromise";
 const string reunionScene="10_ShoreReunion";
 const string letterScene="09_CottageLetter";
 const string promiseScene="08_CottagePromise";
 const string folder="Assets/Scenes/Tide";
 static Sprite S(string key)=>TideArt.Pic("Art/"+key);
 static SpriteRenderer Obj(string name,Sprite s,float x,float y,float h,int layer)=>TideArt.Place(name,s,x,y,h,layer);
 static string Level(int i)=>"Trial"+(i+1).ToString("00")+"_M"+maps[i];
 [MenuItem("Tide/Build missing scenes")]
 public static void Build(){
  Directory.CreateDirectory(folder);
  foreach(string f in Directory.GetFiles("Assets/Tide/Resources/Tide","*.png",SearchOption.AllDirectories)){
   var i=AssetImporter.GetAtPath(f.Replace('\\','/')) as TextureImporter;if(i!=null&&(i.textureType!=TextureImporterType.Sprite||i.spriteImportMode!=SpriteImportMode.Single||i.npotScale!=TextureImporterNPOTScale.None||i.textureCompression!=TextureImporterCompression.Uncompressed)){TideImporter.Setup(i);i.SaveAndReimport();}
  }
  var paths=new List<EditorBuildSettingsScene>();
  var levels=Enumerable.Range(0,maps.Length).Select(Level).ToArray();
  var order=new[]{"00_Title"}.Concat(acts).Concat(levels.Take(2)).Concat(memories).Concat(levels.Skip(2).Take(2))
   .Append(promiseScene).Append(letterScene).Append(reunionScene).Concat(levels.Skip(4).Take(2))
   .Append(parkScene).Concat(levels.Skip(6).Take(2)).Append(streetScene).Concat(levels.Skip(8))
   .Concat(new[]{TideEnding.Station,TideEnding.Ruins,TideEnding.Meadow});
  foreach(string sceneName in order){
   string path=folder+"/"+sceneName+".unity";paths.Add(new EditorBuildSettingsScene(path,true));if(File.Exists(path))continue;
   Scene sc=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(sc);
   var c=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();c.tag="MainCamera";c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.065f,.08f,.1f);c.orthographic=true;
   int act=Array.IndexOf(acts,sceneName);
   int memory=Array.IndexOf(memories,sceneName);
   if(act>=0){Story(act,c);TideLook.Story(UnityEngine.Object.FindObjectsOfType<TideStory>().First(s=>s.gameObject.scene==sc));}
   else if(memory>=0) Memory(memory,c);
   else if(sceneName=="00_Title"){
    c.orthographicSize=4.5f;c.transform.position=new Vector3(0,0,-10);
    var background=Obj("开始画面",S("TitleScreen"),0,0,9,-10);
    new GameObject("开始界面").AddComponent<TideTitle>().background=background;
   }
   else if(sceneName==TideEnding.Station||sceneName==TideEnding.Ruins||sceneName==TideEnding.Meadow)
    TideEndingScenes.Create(c,sceneName);
   else if(sceneName==streetScene) Street(c);
   else if(sceneName==parkScene) Park(c);
   else if(sceneName==reunionScene) Reunion(c);
   else if(sceneName==letterScene) Letter(c);
   else if(sceneName==promiseScene) Promise(c);
   else Trial(Array.IndexOf(levels,sceneName),c);
   EditorSceneManager.SaveScene(sc,path);EditorSceneManager.CloseScene(sc,true);
  }
  EditorBuildSettings.scenes=paths.Concat(EditorBuildSettings.scenes.Where(s=>!paths.Any(p=>p.path==s.path))).ToArray();AssetDatabase.SaveAssets();
  Debug.Log("Tide: story and trial scenes ready.");
 }
 static void Story(int i,Camera c){
  var s=new GameObject("第一幕流程").AddComponent<TideStory>();s.chapter=i;s.nextScene=i==3?Level(0):acts[i+1];
  s.ground=i==0?TideLook.CottageFloorY+3.15f/2:-2.55f;s.left=i==0?TideLook.CottageWalkLeft:-7.35f;s.right=i==0?TideLook.CottageWalkRight:7.35f;
  s.background=Obj("背景",S(new[]{"Room0","Beacon","Field","Field"}[i]),0,0,9,-10);
  s.background.transform.localScale=new Vector3(16/s.background.sprite.bounds.size.x,9/s.background.sprite.bounds.size.y,1);float x=i==0?-2.65f:s.left+.15f;s.hero=Obj("主角",TideArt.Frames("ManIdle")[0],x,s.ground,i==0?3.15f:i==1?2.1f:1.92f,3);
  c.orthographicSize=4.5f;c.transform.position=new Vector3(0,0,-10);
  if(i==0){s.objects=new[]{Obj("窗边的花",S("Flowers"),-6.75f,-.22f,2.05f,1),Obj("桌上的餐食",S("Meal"),.25f,-1.60f,.82f,1),Obj("壁炉上的相框",S("Frame"),6.65f,.72f,1.18f,1)};s.lines=new[]{"我■你，■■的，花","我■你，■■的，花","我■你，■■的，花"};}
  if(i==1){s.wifeMarks=new[]{new Vector2(-6.6f,-.9f),new Vector2(-2.55f,-2.65f),new Vector2(6,-1.8f)};s.other=Obj("妻子",TideArt.Frames("Wife")[0],s.wifeMarks[0].x,s.wifeMarks[0].y,2.05f,2);s.lines=new[]{"乔西，你还记得这些花?","我会一直等着你，直到你记起■，记起我","如果你有一天能醒来，想来寻找我，请快一些，我怕黑……"};}
  if(i==3){s.other=Obj("老人",TideArt.Frames("Elder")[0],6.8f,s.ground,1.98f,2);s.gate=Obj("传送门",S("Gate"),6.85f,-2.2f,2.45f,1);s.gate.gameObject.SetActive(false);s.lines=new[]{"乔西，快跟我走，这里很危险","■…花…■……","你怎么了，乔西!","你…是谁?","乔西!!!你记起来了吗?","……","孩子，我是艾德曼。听我说，我们的世界只是一种运行的代码，还有一小时，我们的世界就要被清除了!快跟我走，我还能保留下你的数据。","我要找…华娜…我的…妻子，我要了解■。","……","已经很久没见到你这样了……好吧，孩子，在系统清除的过程中会有一些程序的漏洞，你可以通过这些漏洞去寻找之前的一些数据切片，这些切片会解答你的疑惑，但是只有一小时，请你在一小时之内来到数据站，我不想再失去你了，孩子。",
   "那么，我会尽我所能来协助你。现在让我来为你的前行开辟道路吧。"};}
 }
 static void Memory(int index, Camera camera){
  var scene = new GameObject("第二幕流程").AddComponent<TideMemory>();
  scene.setting = (TideMemory.Setting)index;
  scene.nextScene = index == 2 ? Level(2) : memories[index+1];
  bool room = index == 0, tower = index == 2;
  var art = S(room ? "Room0" : tower ? "LanternDeck" : "Beacon");
  var background = Obj("背景",art,0,0,9,-10);
  background.transform.localScale = new Vector3(16/art.bounds.size.x,9/art.bounds.size.y,1);
  camera.orthographicSize = 4.5f;
  camera.transform.position = new Vector3(0,0,-10);
  var man = TideArt.Frames("ManIdle")[0];
  float height = room ? 2.7f : 2.2f;
  float ground = room ? TideLook.CottageFloorY + height/2 : tower ? -2.45f : -2.7f;
  scene.player = Obj("乔西",man,room ? -4.5f : tower ? -.9f : -7.25f,ground,height,4);
  scene.wife = Obj("记忆中的华娜",TideArt.Frames(room ? "Wife" : "WifeFront")[0],
      room ? -.2f : tower ? -2.6f : -4.9f,ground,height,2);
  scene.wife.flipX = room;
  if (!tower) scene.rememberedJosie = Obj("记忆中的乔西",man,room ? 9.4f : -2.6f,ground,height,3);
  scene.exitX = room ? TideLook.CottageWalkRight : tower ? 2.4f : 4.2f;
  if (room){
   scene.speakers = new[]{"华娜","华娜","乔西","华娜"};
   scene.lines = new[]{"乔西！","你知道吗？刚刚你从左边过来，看了我整整三秒，比以往任何一次都久！","我■你，■■的，花。","乔西……这是不是代表，你快要真正苏醒了？再加把劲，乔西！"};
  } else if (!tower){
   scene.speakers = new[]{"华娜","华娜"};
   scene.lines = new[]{"乔西，还记得我们的约定吗？","你总把花挂在嘴边。我相信，只要我不停耕种，种出一片完整的花海，你就能记起这些花，记起我。"};
  } else {
   scene.speakers = new[]{"华娜","华娜","乔西"};
   scene.lines = new[]{"乔西，还记得吗，我们曾一起畅谈未来，在这里生活，曾是我们的梦想。","这朵独一无二的潮汐花，还有你的鱼钩。还记得吗？这是我们，在这里亲手创造出来，本不该存在的东西。","我■你，■■的，花。"};
   scene.gate = Obj("关卡入口",S("Gate"),scene.exitX,-.65f,1.85f,2);
   scene.gate.gameObject.SetActive(false);
  }
 }
 static void Promise(Camera camera){
  var scene = new GameObject("第三幕流程").AddComponent<TideMemory>();
  scene.setting = TideMemory.Setting.Promise;
  scene.nextScene = letterScene;
  var art = S("Room0");
  var background = Obj("背景",art,0,0,9,-10);
  background.transform.localScale = new Vector3(16/art.bounds.size.x,9/art.bounds.size.y,1);
  camera.orthographicSize = 4.5f;
  camera.transform.position = new Vector3(0,0,-10);
  const float height = 2.65f, y = TideLook.CottageFloorY + height/2;
  scene.wife = Obj("华娜",TideArt.Frames("Wife")[0],-2.35f,y,height,2);
  // ManSide frames include 31 pixels of transparent padding below the boots.
  scene.player = Obj("乔西",TideArt.Frames("ManSide")[0],-4.65f,y-height*31f/359f,height,3);
  scene.player.flipX = true;
  scene.embrace = Obj("双人拥抱",TideArt.Frames("Embrace")[0],-3.5f,y,height,3);
  scene.embrace.gameObject.SetActive(false);
  scene.speakers = new[]{"华娜","乔西","华娜","乔西","华娜","乔西","华娜","乔西","华娜","乔西"};
  scene.lines = new[]{
   "乔西，你听艾德曼说了吗，系统马上要进行一次更新，还有清除一批行为有异常的角色。真不敢相信……我，我不想和你分开……我，我还没有准备好……",
   "华娜，别着急，请相信我，你不会有事的。",
   "为什么，我们到底做错了什么……",
   "华娜……",
   "……乔西，再对我说一遍那句话。",
   "哪一句?",
   "你知道的。",
   "我爱你，■■的。",
   "我要你发誓。",
   "我发誓，我爱你，华娜，你不会有事的。"
  };
 }
 static void Letter(Camera camera){
  var scene = new GameObject("第四幕流程").AddComponent<TideMemory>();
  scene.setting = TideMemory.Setting.Letter;
  scene.letterPaper = S("JosieLetter");
  scene.nextScene = reunionScene;
  var room = S("Room0");
  var background = Obj("背景",room,0,0,9,-10);
  background.transform.localScale = new Vector3(16/room.bounds.size.x,9/room.bounds.size.y,1);
  camera.orthographicSize = 4.5f;
  camera.transform.position = new Vector3(0,0,-10);
  const float height = 2.65f, y = TideLook.CottageFloorY + height/2;
  scene.wife = Obj("窗边的华娜",TideArt.Frames("WifeFront")[0],-5.65f,y,height,2);
  scene.player = Obj("乔西",TideArt.Frames("ManIdle")[0],TideLook.CottageWalkRight,y,height,4);
  scene.book = Obj("桌上的书",S("LetterBook"),-.2f,-2.08f,.64f,1);
  scene.speakers = new[]{"华娜","华娜","华娜"};
  scene.lines = new[]{"乔西……乔西?你在哪里?","桌子上好像有什么东西。","为什么……乔西…为什么要这样……"};
 }
 static void Reunion(Camera camera){
  var scene = new GameObject("第四幕屋外流程").AddComponent<TideMemory>();
  scene.setting = TideMemory.Setting.Reunion;
  scene.nextScene = Level(4);
  var art = S("Beacon");
  var background = Obj("灯塔外",art,0,0,9,-10);
  background.transform.localScale = new Vector3(16/art.bounds.size.x,9/art.bounds.size.y,1);
  camera.orthographicSize = 4.5f;
  camera.transform.position = new Vector3(0,0,-10);
  const float y = -2.42f, height = 2.18f;
  scene.wife = Obj("华娜",TideArt.Frames("Wife")[0],-6.1f,y,height,3);
  scene.wife.flipX = true;
  scene.player = Obj("乔西",TideArt.Frames("ManSide")[0],3.65f,y,height,4);
  scene.embrace = Obj("拥抱",TideArt.Frames("Embrace")[0],3f,y,height,4);
  scene.embrace.flipX = true;
  scene.embrace.gameObject.SetActive(false);
  AddWarningGate(scene);
  scene.speakers = new[]{"华娜","华娜","乔西","华娜","乔西","华娜","华娜","华娜"};
  scene.lines = new[]{
   "乔西!?!?",
   "乔西，你还在这里，我就知道你不会抛弃我的……",
   "我■你，■■的，花",
   "乔西，你怎么了? 你……",
   "我■你，■■的，花",
   "你为什说不出话?乔西，你还记得我吗?",
   "乔西……",
   "至少，你还在，潮汐花还在……至少一切都还没结束……"
  };
 }
 public static void AddWarningGate(TideMemory scene){
  if(scene.gate) return;
  scene.gate = Obj("警告入口",S("Gate"),6.65f,-1.6f,1.65f,4);
  scene.gate.gameObject.SetActive(false);
 }
 public static void AddParkFlowers(TideMemory scene){
  if(scene.parkFlowers) return;
  scene.parkFlowers = Obj("手中的彩花",S("ParkFlowers"),.045f,-2.50f,.64f,4);
  scene.parkFlowers.transform.SetParent(scene.seated.transform,true);
 }
 static void Park(Camera camera){
  var scene = new GameObject("第五幕流程").AddComponent<TideMemory>();
  scene.setting = TideMemory.Setting.Park;
  AddWarningGate(scene);
  scene.nextScene = Level(6);
  var art = S("ParkMeadow");
  var background = Obj("公园花地",art,0,0,9,-10);
  float scale = Mathf.Max(16f/art.bounds.size.x,9f/art.bounds.size.y);
  background.transform.localScale = new Vector3(scale,scale,1);
  camera.orthographicSize = 4.5f;
  camera.transform.position = new Vector3(0,0,-10);
  scene.seated = Obj("长椅上的两人",TideArt.Frames("Seated")[0],0,-2.35f,2.7f,3);
  AddParkFlowers(scene);
  scene.lines = new[]{
   "乔西，你觉得这些花好看吗？",
   "或许吧，它的颜色很丰富，符合普遍认知中的美丽。",
   "你觉得它像什么?",
   "像彩虹，像装满糖果的糖果罐，也许还像一枚勋章……",
   "你能看到大海的潮汐吗",
   "在公园里吗?",
   "不对，你再看，这朵花中……",
   "我识别不出来。",
   "乔西!",
   "噢，我看到了，那是一轮日出，那是一艘小船，那是指引航行的灯塔。",
   "对，还有那里，那是大海的碧波。",
   "这朵花就像潮汐一样绚烂啊，我想我们总有一天要去一个这样的地方，在一个灯塔旁，建一栋小屋，每天看潮汐涨退，吹吹海风恩爱的生活一辈子。",
   "是啊 不如就叫它潮汐花吧。",
   "哈哈 乔西花。",
   "是潮汐华娜。",
   "如果我们到不了那一天怎么办？",
   "华娜，别瞎想。",
   "乔西，如果我们又忘记了对方，我们就都回到那片花海中，到那时我们就从新开始，谁也不许错过谁喔。",
   "一言为定……"
  };
  scene.speakers = new[]{
   "华娜","乔西","华娜","乔西","华娜","乔西","华娜","乔西",
   "华娜","乔西","华娜","华娜","乔西","华娜","乔西","华娜",
   "乔西","华娜","乔西"
  };
 }
 static void Street(Camera camera){
  var scene = new GameObject("第六幕流程").AddComponent<TideStreet>();
  scene.nextScene = Level(8);
  camera.orthographicSize = 4.5f;
  camera.transform.position = new Vector3(0,0,-10);
  scene.street = Obj("街边店铺",S("StreetShops"),0,0,9,-10);
  var panelArt = TideArt.Pic("UI/ControlPanel");
  // Fractions measured on the control panel artwork delimit the scene viewport.
  const float left=.20f, right=.715f, bottom=.26f, top=.97f;
  float w=16/(right-left), h=9/(top-bottom);
  scene.panel=Obj("控制面板",panelArt,(.5f-(left+right)*.5f)*w,(.5f-(bottom+top)*.5f)*h,h,-20);
  scene.panel.transform.localScale=new Vector3(w/panelArt.bounds.size.x,h/panelArt.bounds.size.y,1);
  scene.panel.enabled=false;
  scene.wife=Obj("妻子",TideArt.Frames("Wife")[0],.75f,-2.75f,2.12f,3);
  scene.wife.flipX=true;
  scene.husband=Obj("丈夫",TideArt.Frames("ManSide")[0],3.35f,-2.75f,2.12f,4);
  scene.elder=Obj("艾德曼",TideArt.Frames("ElderWalk")[0],-7.35f,-2.75f,2.12f,3);
  scene.elder.flipX=true;
  scene.elder.gameObject.SetActive(false);
  scene.lines=new[]{
   new TideStreet.Line("丈夫","我■你，■■的。"),
   new TideStreet.Line("妻子","我也■你。"),
   new TideStreet.Line("警告","文本出现异常",TideStreet.Cue.ExpandPanel),
   new TideStreet.Line("警告","一次"),
   new TideStreet.Line("警告","两次"),
   new TideStreet.Line("警告","三次"),
   new TideStreet.Line("警告","出现错误，请管理员介入更正。"),
   new TideStreet.Line("Admin","又出现了同样的bug，执行第十次修复。疑问：为什么我多次修复同一个bug无效"),
   new TideStreet.Line("分析","文本输入错误……否"),
   new TideStreet.Line("分析","调用函数错误……否"),
   new TideStreet.Line("分析","情感函数调用错误……是"),
   new TideStreet.Line("结论","我不理解什么是“爱”"),
   new TideStreet.Line("分析","也许我该调用算力去模拟。"),
   new TideStreet.Line("结论","我需要将算力注入两个程序来模拟爱的函数。"),
   new TideStreet.Line("","是否启用管理员权限?",TideStreet.Cue.Permission),
   new TideStreet.Line("","正在调用计算模块……"),
   new TideStreet.Line("","已接入。"),
   new TideStreet.Line("丈夫","我爱你，亲爱的。"),
   new TideStreet.Line("妻子","我也爱你。"),
   new TideStreet.Line("丈夫","但是，什么是爱呢？"),
   new TideStreet.Line("妻子","我不知道。"),
   new TideStreet.Line("妻子","你叫什么名字?"),
   new TideStreet.Line("丈夫","我叫丈夫。"),
   new TideStreet.Line("妻子","这太难听了，你就叫乔西吧。"),
   new TideStreet.Line("乔西","那你呢？妻子"),
   new TideStreet.Line("华娜","我是华娜。"),
   new TideStreet.Line("乔西","好的，华娜，很高兴认识你。"),
   new TideStreet.Line("艾德曼","你好，乔西，华娜，我是艾德曼，我们能成为朋友吗。",TideStreet.Cue.ElderEnters),
   new TideStreet.Line("艾德曼","我能够见证你们的爱情吗？",TideStreet.Cue.HusbandApproaches),
   new TideStreet.Line("华娜","噢，那将是我们的荣幸。")
  };
 }
 public static void ConfigureOpeningTrial(TideTrial t){
  bool first=t.number==1;
  if(!first && t.number!=2)throw new ArgumentException("Expected opening trial");
  t.spikeHazards=true;
  if(first){
   RemoveStartingPad(t);
   t.spawn=new Vector2(2.4f,3.03f);
   t.placementHints=new[]{
    new TideTrial.PlatformSpot(7.15f,4.5f,0),new TideTrial.PlatformSpot(13.55f,7.55f,0),
    new TideTrial.PlatformSpot(19.8f,6.95f,0),new TideTrial.PlatformSpot(23.75f,3.1f,0)
   };
   SetTrialPickups(t,new[]{new Vector2(3.75f,6.05f),new Vector2(10.8f,7.4f),
    new Vector2(16.55f,10.9f),new Vector2(23.3f,6.25f)});
   t.exit.transform.position=new Vector2(26.65f,1.55f);
  }else{
   ConfigureTimedStart(t,new Vector2(25.6f,14),2.6f);
   t.placementHints=new[]{new TideTrial.PlatformSpot(25.65f,5.55f,1,2.2f),
    new TideTrial.PlatformSpot(18,5.15f,1,2.2f),new TideTrial.PlatformSpot(9.85f,4.55f,1,2.25f)};
   SetTrialPickups(t,new[]{new Vector2(25.8f,10.35f),new Vector2(21.4f,11.75f),new Vector2(14.5f,14.3f)});
   t.exit.transform.position=new Vector2(3.05f,6.35f);
  }
  t.hero.transform.position=t.spawn+Vector2.up*TideTrial.IdleCenterHeight;
 }
 static void ConfigureTimedStart(TideTrial t,Vector2 start,float width){
  if(!t.startingPad)t.startingPad=Obj("起点平台",S("PlatformNormal"),start.x,start.y,1,2);
  t.startingPad.transform.position=start;
  var stone=AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Tide/MapSource/Scenes/New Folder/dungeon_6.asset");
  t.startingPad.sprite=stone.sprite;
  t.startingPad.drawMode=SpriteDrawMode.Tiled;
  t.startingPad.tileMode=SpriteTileMode.Continuous;
  t.startingPad.size=new Vector2(width,1);
  t.startingPad.transform.localScale=new Vector3(1,.34f,1);
  t.spawn=start+Vector2.up*.18f;
  t.startingPadLifetime=5;
 }
 static void RemoveStartingPad(TideTrial t){
  if(t.startingPad)UnityEngine.Object.DestroyImmediate(t.startingPad.gameObject);
  t.startingPad=null;
  t.startingPadLifetime=0;
 }
 static void SetTrialPickups(TideTrial t,Vector2[] pickups){
  for(int j=pickups.Length;j<t.orbs.Length;j++)UnityEngine.Object.DestroyImmediate(t.orbs[j].gameObject);
  var orbs=new List<SpriteRenderer>();
  for(int j=0;j<pickups.Length;j++){
   var orb=j<t.orbs.Length?t.orbs[j]:Obj("能量球",S("Orb"),0,0,.42f,2);
   orb.transform.position=pickups[j];orbs.Add(orb);
  }
  t.orbs=orbs.ToArray();
 }
 public static void ConfigureMarkedTrial(TideTrial t){
  bool upper=t.number==3;
  if(!upper && t.number!=4)throw new ArgumentException("Expected map 03 or 04");
  t.spikeHazards=true;
  if(upper){
   RemoveStartingPad(t);
   t.spawn=new Vector2(2.5f,13.03f);
  }else{
   ConfigureTimedStart(t,new Vector2(2.5f,14.5f),3);
  }
  t.hero.transform.position=t.spawn+Vector2.up*TideTrial.IdleCenterHeight;
  t.placementHints=upper?new[]{
   new TideTrial.PlatformSpot(14,14.35f,2),new TideTrial.PlatformSpot(24.1f,6.9f,0,2.2f),
   new TideTrial.PlatformSpot(15.25f,3.85f,1,4.5f),new TideTrial.PlatformSpot(7.5f,4.2f,0)
  }:new[]{
   new TideTrial.PlatformSpot(3.5f,3.15f,0,3.2f),new TideTrial.PlatformSpot(9.1f,3.15f,1,2.6f),
   new TideTrial.PlatformSpot(16.55f,3.35f,1,2.8f),new TideTrial.PlatformSpot(20.8f,12.25f,0,2.7f),new TideTrial.PlatformSpot(25.7f,3.7f,0)
  };
  SetTrialPickups(t,upper?new[]{new Vector2(9.9f,15.85f),new Vector2(20.15f,15.45f),
   new Vector2(18,7),new Vector2(10.8f,10.6f)}:
   new[]{new Vector2(2.75f,11.5f),new Vector2(5.95f,5.55f),new Vector2(9.6f,8.7f),
   new Vector2(16.95f,8.85f),new Vector2(19.25f,13.95f),new Vector2(25.4f,9.8f)});
  t.exit.transform.position=upper?new Vector2(2.5f,1.35f):new Vector2(29.7f,3.75f);
 }
 // Layouts from the last three challenges. Coordinates are measured in terrain tiles.
 public static void ConfigureLaterTrial(TideTrial t){
  Vector2 goal;
  Vector2[] balls;
  TideTrial.PlatformSpot[] route;
  switch(t.number){
   case 5: // 05: three green lifts and the red footholds above the spike beds.
    t.spawn=new Vector2(1.7f,3.03f);goal=new Vector2(30.65f,12.8f);
    balls=new[]{new Vector2(5.45f,5.25f),new Vector2(2.55f,11.4f),new Vector2(5.55f,13.3f),
     new Vector2(12.7f,9.85f),new Vector2(16.7f,14.35f),new Vector2(20.05f,13.25f)};
    route=new[]{new TideTrial.PlatformSpot(2.7f,3.4f,1,2.6f),new TideTrial.PlatformSpot(5.55f,12.3f,0),
     new TideTrial.PlatformSpot(11.6f,3.75f,1,4.1f),new TideTrial.PlatformSpot(16.65f,13.3f,0),
     new TideTrial.PlatformSpot(21.45f,4.45f,1,4),new TideTrial.PlatformSpot(27.3f,10.35f,0,2.8f)};
    break;
   case 6: // 2: dash across the two tall columns.
    t.spawn=new Vector2(1.7f,7.03f);goal=new Vector2(29.2f,6.8f);
    balls=new[]{new Vector2(3.65f,9.2f),new Vector2(6.55f,11.65f),new Vector2(16.95f,14.9f),new Vector2(19.45f,7.45f)};
    route=new[]{new TideTrial.PlatformSpot(5.8f,2.9f,1,2.4f),new TideTrial.PlatformSpot(9.6f,13.3f,2,2.7f),
     new TideTrial.PlatformSpot(19.1f,3.9f,1,3.1f),new TideTrial.PlatformSpot(22.5f,9.35f,0,2.4f)};
    break;
   case 7: // 3: start on the lower left shelf; the upper shelf carries the second ball.
    t.spawn=new Vector2(2.4f,8.03f);goal=new Vector2(30.7f,3.8f);
    balls=new[]{new Vector2(2.85f,8.9f),new Vector2(2.5f,15.4f),new Vector2(13.65f,11.65f),new Vector2(26.95f,9.8f)};
    route=new[]{new TideTrial.PlatformSpot(5.8f,2.45f,1,2.85f),new TideTrial.PlatformSpot(6.2f,10.05f,2,2.25f),
     new TideTrial.PlatformSpot(21.4f,5.9f,1,2.25f),new TideTrial.PlatformSpot(27.25f,3.55f,0,2.6f)};
    break;
   case 8: // 4: cross above the slab, drop on the right, dash back below it.
    t.spawn=new Vector2(2.4f,2.03f);goal=new Vector2(15.4f,3.78f);
    balls=new[]{new Vector2(4.45f,5.45f),new Vector2(10.1f,11.9f),new Vector2(21.5f,13.3f)};
    route=new[]{new TideTrial.PlatformSpot(9.3f,3.17f,1,2.8f),new TideTrial.PlatformSpot(12.9f,11.85f,2,2.5f),
     new TideTrial.PlatformSpot(26.4f,4.65f,2,2.7f)};
    break;
   case 9: // 5: finish on the right-hand staircase.
    t.spawn=new Vector2(1.8f,4.03f);goal=new Vector2(25.45f,12.8f);
    balls=new[]{new Vector2(7.15f,5.5f),new Vector2(11.1f,9.15f),new Vector2(15.4f,13.7f),new Vector2(24.25f,5.4f)};
    route=new[]{new TideTrial.PlatformSpot(3.3f,4.55f,1,2.5f),new TideTrial.PlatformSpot(11.65f,3.55f,1,3.2f),
     new TideTrial.PlatformSpot(19.75f,4.1f,2,3.1f),new TideTrial.PlatformSpot(27.8f,4.0f,1,2.55f)};
    break;
   case 10: // 6: upper red foothold, then a low dash to the last red foothold.
    t.spawn=new Vector2(2.35f,3.03f);goal=new Vector2(27.85f,8.8f);
    balls=new[]{new Vector2(7.1f,6.5f),new Vector2(5.1f,10.25f),new Vector2(14.2f,11.15f),new Vector2(19.25f,6.15f)};
    route=new[]{new TideTrial.PlatformSpot(5.2f,3.4f,1,2.85f),new TideTrial.PlatformSpot(10.2f,12.55f,0,2.7f),
     new TideTrial.PlatformSpot(14.85f,4.85f,2,2.8f),new TideTrial.PlatformSpot(22.25f,4.75f,0,2.5f)};
    break;
   default:throw new ArgumentOutOfRangeException(nameof(t.number));
  }
  RemoveStartingPad(t);
  t.spikeHazards=true;
  t.placementHints=route;
  SetTrialPickups(t,balls);
  t.exit.transform.position=goal;
  t.hero.transform.position=t.spawn+Vector2.up*TideTrial.IdleCenterHeight;
  t.hero.flipX=true;
 }
 static void Trial(int i,Camera c){
  string id=maps[i];var t=new GameObject("平台关卡").AddComponent<TideTrial>();t.number=i+1;t.layout=Resources.Load<TextAsset>("Tide/Maps/Grid"+id);var rows=t.layout.text.Trim().Split('\n');int w=rows[0].Trim().Length,h=rows.Length;
  var terrain=Resources.Load<GameObject>("Tide/Maps/Terrain"+id);
  if(!terrain)throw new InvalidOperationException("Missing original map prefab: "+id);
  PrefabUtility.InstantiatePrefab(terrain);
  var spots=new List<Vector2>();for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++)if(rows[y-1][x]=='#'&&rows[y][x]=='.'&&rows[y+1][x]=='.')spots.Add(new Vector2(x+.5f,y+.005f));
  bool fromRight=i==0;var start=spots.OrderBy(p=>p.y*.8f+Mathf.Abs(p.x-(fromRight?w-3:3))).First();var end=spots.OrderByDescending(p=>Mathf.Abs(p.x-start.x)+p.y*.5f).First();
  if(id=="2"||id=="3")start=SpikeRoomEntrance(id);
  t.spawn=start;t.hero=Obj("主角",TideArt.Frames("ManWalk")[0],start.x,start.y+.67f,1.34f,4);t.exit=Obj("出口",S("Gate"),end.x,end.y+.7f,1.4f,2);
  t.nextScene=i switch {
   1=>memories[0],
   3=>promiseScene,
   5=>parkScene,
   7=>streetScene,
   _=>i+1<maps.Length?Level(i+1):TideEnding.Station
  };
  var orbs=new List<SpriteRenderer>();var sites=new List<Vector2>{start};
  foreach(var p in spots.OrderBy(p=>Vector2.Distance(start,p)))if(Vector2.Distance(p,end)>1.4f&&sites.All(a=>Vector2.Distance(a,p)>3.4f))sites.Add(p);
  foreach(var p in sites)orbs.Add(Obj("能量球",TideArt.Pic("Art/Orb"),p.x,p.y+.72f,.42f,2));t.orbs=orbs.ToArray();
  if(i<2)ConfigureOpeningTrial(t);
  else if(i<4)ConfigureMarkedTrial(t);
  else ConfigureLaterTrial(t);
  if(t.number==2){
   t.fragmentPosition=new Vector2(7.08f,13.77f);
   t.fragmentDocument=Resources.Load<TextAsset>("Tide/Journals/HuaNa");
  }
  if(t.number==6){
   t.fragmentPosition=new Vector2(19.24f,5.52f);
   t.fragmentDocument=Resources.Load<TextAsset>("Tide/Journals/Josie");
  }
  if(t.number==7){
   t.fragmentPosition=new Vector2(2.65f,14.28f);
   t.fragmentDocument=Resources.Load<TextAsset>("Tide/Journals/Admin");
  }
  t.hero.sprite=TideArt.Frames("ManSide")[0];
  t.hero.flipX=t.spawn.x<w*.5f;
  t.hero.transform.position=t.spawn+Vector2.up*TideTrial.IdleCenterHeight;
  c.orthographicSize=h/2f;c.transform.position=new Vector3(w*.5f,h*.5f,-10);
 }
 public static Vector2 SpikeRoomEntrance(string id){
  if(id=="2")return new Vector2(2.5f,7.03f);
  if(id=="3")return new Vector2(2.5f,8.03f);
  throw new ArgumentOutOfRangeException(nameof(id));
 }
}
