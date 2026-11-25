using UnityEngine;

// Shared composition rules used in edit mode and at runtime.
public static class TideLook {
 // In the cottage artwork the clear floor runs between the cabinet and the hearth.
 public const float CottageFloorY = -4.32f, CottageWalkLeft = -5.75f, CottageWalkRight = 4.60f;
 public const float WoodStepVolume = .78f;
 public static void Story(TideStory s) {
  bool room=s.chapter==0;
  Background(s);
  s.left=room?CottageWalkLeft:-7.35f;s.right=room?CottageWalkRight:7.35f;
  s.ground=room?CottageFloorY+3.15f/2:-2.55f;
  var p=s.hero.transform.position;p.y=s.ground;s.hero.transform.position=p;
  TideArt.Fit(s.hero,s.hero.sprite,room?3.15f:1.85f);
  if(room&&s.objects!=null&&s.objects.Length==3){
   Place(s.objects[0],-6.65f,-.25f,2.35f);
   Place(s.objects[1],0,-1.90f,.80f);
   Place(s.objects[2],6.65f,.62f,1.15f);
  }
  if(s.chapter==1){
   FitHusbandAtFeet(s.hero,s.hero.sprite,1.95f,s.hero.bounds.min.y);
   s.wifeMarks=new[]{new Vector2(-5.96f,0),new Vector2(-2.7f,-2.75f),new Vector2(6.7f,-1.15f)};
   if(s.other){s.other.transform.position=s.wifeMarks[0];s.other.flipX=false;}
  }
  if(s.other)TideArt.Fit(s.other,s.other.sprite,s.chapter==1?1.85f:1.9f);
 }
 public static void Background(TideStory s){
  TideArt.Fit(s.background,s.background.sprite,9);
  if(s.chapter==0)s.background.transform.localScale=new Vector3(16/s.background.sprite.bounds.size.x,9/s.background.sprite.bounds.size.y,1);
 }
 // The husband's side-view idle cells are 359 px tall, including about 31 px
 // below the soles. Use one silhouette envelope for the full animation to retain breathing.
 public static void PoseHusband(SpriteRenderer husband,Sprite frame,SpriteRenderer wife,float height,bool sideIdle){
  FitHusbandAtFeet(husband,frame,height,wife.bounds.min.y,sideIdle);
 }
 public static void FitHusbandAtFeet(SpriteRenderer husband,Sprite frame,float wifeHeight,float floor,bool sideIdle=false){
  float height=wifeHeight*1.06f;
  float frameHeight=sideIdle?height*359f/328f:height;
  TideArt.Fit(husband,frame,frameHeight);
  float foot=husband.transform.TransformPoint(frame.bounds.min).y+(sideIdle?31f/359f*frameHeight:0f);
  husband.transform.position+=Vector3.up*(floor-foot);
 }
 static void Place(SpriteRenderer r,float x,float y,float h){r.transform.position=new Vector3(x,y,0);TideArt.Fit(r,r.sprite,h);}
 public static System.Collections.IEnumerator WalkToX(Transform actor,float targetX,float speed) {
  // Shoe alignment changes the sprite's Y when a walk switches animation frames.
  while(actor.position.x!=targetX){
   var position=actor.position;
   position.x=Mathf.MoveTowards(position.x,targetX,speed*Time.deltaTime);
   actor.position=position;
   yield return null;
  }
 }
 // Restore the tint after hiding so a later memory can reuse the same actor.
 public static System.Collections.IEnumerator FadeOut(SpriteRenderer actor, System.Action onHidden = null) {
  const float duration = 1.6f;
  Color tint = actor.color;
  var shadow = actor.GetComponentInChildren<MeshRenderer>();
  Material shadowMaterial = shadow ? shadow.sharedMaterial : null;
  Color shadowTint = shadowMaterial ? shadowMaterial.color : Color.clear;
  float start = Time.time;
  while (Time.time - start < duration) {
   float opacity = 1f - Mathf.SmoothStep(0f, 1f, (Time.time - start) / duration);
   actor.color = new Color(tint.r, tint.g, tint.b, tint.a * opacity);
   if (shadowMaterial)
    shadowMaterial.color = new Color(shadowTint.r, shadowTint.g, shadowTint.b, shadowTint.a * opacity);
   yield return null;
  }
  actor.gameObject.SetActive(false);
  actor.color = tint;
  if (shadowMaterial) shadowMaterial.color = shadowTint;
  onHidden?.Invoke();
 }
 public static void Shadow(Transform person,float width,float offset){
  var child=new GameObject("脚下阴影");child.transform.SetParent(person,false);
  // Use a small procedural mesh so no new raster art is required.
  var mesh=new Mesh();var vertices=new Vector3[33];var indices=new int[96];vertices[0]=Vector3.zero;
  for(int i=0;i<32;i++){float a=i*Mathf.PI/16;vertices[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=(i+1)%32+1;}
  mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateBounds();child.AddComponent<MeshFilter>().mesh=mesh;
  var renderer=child.AddComponent<MeshRenderer>();renderer.material=new Material(Shader.Find("Sprites/Default"));renderer.material.color=new Color(.10f,.075f,.04f,.25f);renderer.sortingOrder=1;
  float scale=person.localScale.x;child.transform.localPosition=new Vector3(0,offset/scale,.01f);child.transform.localScale=new Vector3(width/scale,.075f/scale,1);
 }
}
