using UnityEngine;

// Visuals follow the movement clock. Collision always uses the authored platform dimensions.
public sealed class TideTrialFeedback {
 const float SpringDuration=.18f, EchoLife=.12f, EchoInterval=.045f;
 readonly SpriteRenderer hero;
 readonly SpriteRenderer[] echoes=new SpriteRenderer[3];
 readonly float[] ages=new float[3];
 int nextEcho;
 float echoDelay,springTime;
 SpriteRenderer spring;
 Vector3 springScale;

 public TideTrialFeedback(SpriteRenderer hero,Transform parent){
  this.hero=hero;
  for(int i=0;i<echoes.Length;i++){
   var copy=new GameObject("冲刺残影").AddComponent<SpriteRenderer>();
   copy.transform.SetParent(parent,false);
   copy.sortingLayerID=hero.sortingLayerID;
   copy.sortingOrder=hero.sortingOrder-1;
   copy.enabled=false;
   echoes[i]=copy;
  }
 }

 public void Reset(){
  if(spring)spring.transform.localScale=springScale;
  spring=null;
  echoDelay=0;
  foreach(var echo in echoes)echo.enabled=false;
 }

 public void Bounce(SpriteRenderer pad){
  if(spring)spring.transform.localScale=springScale;
  spring=pad;
  springScale=pad.transform.localScale;
  springTime=0;
 }

 public void Tick(float dt,bool dashing){
  if(spring){
   springTime+=dt;
   float progress=Mathf.Clamp01(springTime/SpringDuration);
   float compression=progress<.25f?progress*4:(1-progress)/.75f;
   spring.transform.localScale=Vector3.Scale(springScale,new Vector3(1+.1f*compression,1-.38f*compression,1));
   if(progress>=1)spring=null;
  }
  for(int i=0;i<echoes.Length;i++){
   if(!echoes[i].enabled)continue;
   ages[i]+=dt;
   echoes[i].enabled=ages[i]<EchoLife;
   echoes[i].color=Tint(ages[i]);
  }
  if(!dashing){echoDelay=0;return;}
  echoDelay-=dt;
  if(echoDelay>0)return;
  echoDelay=EchoInterval;
  var echo=echoes[nextEcho];
  echo.sprite=hero.sprite;
  echo.flipX=hero.flipX;
  echo.transform.SetPositionAndRotation(hero.transform.position,hero.transform.rotation);
  echo.transform.localScale=hero.transform.lossyScale;
  echo.color=Tint(0);
  echo.enabled=true;
  ages[nextEcho]=0;
  nextEcho=(nextEcho+1)%echoes.Length;
 }
 static Color Tint(float age)=>new Color(.78f,.56f,1f,.52f*Mathf.Clamp01(1-age/EchoLife));
}
