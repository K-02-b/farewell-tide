using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class TideMapRestore {
 public static string Restore() => RestoreMaps(false);
 public static string RestoreOpeningPair() => RestoreMaps(true);
 public static string RefreshOpeningTerrainAssets() => RestoreMaps(true,false);
 public static string RefreshTerrainAssets() => RestoreMaps(false,false);
 static string RestoreMaps(bool openingOnly,bool replaceScenes=true) {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode first.");
  string[] ids={"0","01","03","04","05","2","3","4","5","6"};
  var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Tide/MapSource/OriginalMap.mat");
  if(!material){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,"Assets/Tide/MapSource/OriginalMap.mat");}
  int total=0;
  int count=openingOnly?2:ids.Length;
  for(int i=0;i<count;i++) {
   string id=ids[i];
   var source=EditorSceneManager.OpenScene("Assets/Tide/MapSource/Scenes/"+id+".unity",OpenSceneMode.Additive);
   var maps=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>()).ToArray();
   var occupied=new System.Collections.Generic.List<Vector3Int>();
   foreach(var map in maps)foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p))occupied.Add(p);
   var origin=new Vector3Int(occupied.Min(p=>p.x),occupied.Min(p=>p.y),0);
   int w=occupied.Max(p=>p.x)-origin.x+1,h=occupied.Max(p=>p.y)-origin.y+1;
   var root=new GameObject("原始地图 "+id,typeof(Grid));SceneManager.MoveGameObjectToScene(root,source);
   var spikeVertices=new System.Collections.Generic.List<Vector3>();
   var spikeUV=new System.Collections.Generic.List<Vector2>();
   var spikeTriangles=new System.Collections.Generic.List<int>();
   Sprite spikeArt=null;
   var spans=new List<Rect>();
   foreach(var map in maps) {
    var go=new GameObject("原始砖块",typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(root.transform,false);
    var target=go.GetComponent<Tilemap>();var renderer=go.GetComponent<TilemapRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=-2;
    target.tileAnchor=map.tileAnchor;
    foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p)) {
     var q=p-origin;
     var sprite=map.GetSprite(p);
     if(IsSpike(sprite)) {
      if(id=="01" && q.x<6 && q.y==2)continue;
      spikeArt=sprite;
      // Clip the oversized artwork at neighbouring wall cells, without stretching it.
      var center=(Vector3)(p-origin)+map.tileAnchor;
      float x0=center.x+sprite.bounds.min.x,x1=center.x+sprite.bounds.max.x;
      float left=x0,right=x1;
      // Also stop at the ends of the supporting ledge, even when there is no side wall.
      if(HasGround(maps,new Vector3Int(p.x,p.y-1,p.z))) {
       int first=p.x,last=p.x;
       while(HasGround(maps,new Vector3Int(first-1,p.y-1,p.z)))first--;
       while(HasGround(maps,new Vector3Int(last+1,p.y-1,p.z)))last++;
       left=Mathf.Max(left,first-origin.x);right=Mathf.Min(right,last-origin.x+1);
      }
      foreach(var other in maps)for(int x=Mathf.FloorToInt(x0)+origin.x;x<=Mathf.FloorToInt(x1)+origin.x;x++) {
       var cell=new Vector3Int(x,p.y,p.z);
       if(other.HasTile(cell)&&!IsSpike(other.GetSprite(cell))) {
        if(x<p.x)left=Mathf.Max(left,x-origin.x+1);
        if(x>p.x)right=Mathf.Min(right,x-origin.x);
       }
      }
      if(right>left)spans.Add(new Rect(left,q.y,right-left,0));
      total++;continue;
     }
     target.SetTile(q,map.GetTile(p));target.SetTileFlags(q,TileFlags.None);target.SetTransformMatrix(q,map.GetTransformMatrix(p));target.SetColor(q,map.GetColor(p));total++;
    }
    if(id=="01" && target.HasTile(new Vector3Int(1,1,0)))BuildOpeningExitLedges(target);
    go.AddComponent<TilemapCollider2D>().ProcessTilemapChanges();
   }
   foreach(var row in spans.GroupBy(r=>r.y)){
    Rect run=default;bool started=false;
    foreach(var part in row.OrderBy(r=>r.xMin)){
     if(started&&part.xMin<=run.xMax+.001f){run.xMax=Mathf.Max(run.xMax,part.xMax);continue;}
     if(started)AddSpikeRun(root,spikeArt,run,spikeVertices,spikeUV,spikeTriangles);
     run=part;started=true;
    }
    if(started)AddSpikeRun(root,spikeArt,run,spikeVertices,spikeUV,spikeTriangles);
   }
   if(id=="05")AddExitGate(root,spikeArt,spikeVertices,spikeUV,spikeTriangles);
   if(spikeVertices.Count>0) {
    string meshPath="Assets/Tide/Resources/Tide/Maps/Terrain"+id+"Spikes.asset";
    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
    mesh.SetVertices(spikeVertices);mesh.SetUVs(0,spikeUV);mesh.SetTriangles(spikeTriangles,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
    const string spikeMaterialPath="Assets/Tide/MapSource/Spikes.mat";
    var spikeMaterial=AssetDatabase.LoadAssetAtPath<Material>(spikeMaterialPath);
    if(!spikeMaterial){spikeMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(spikeMaterial,spikeMaterialPath);}
    spikeMaterial.mainTexture=spikeArt.texture;EditorUtility.SetDirty(spikeMaterial);
    var spikes=new GameObject("边缘裁切尖刺",typeof(MeshFilter),typeof(MeshRenderer));spikes.transform.SetParent(root.transform,false);
    spikes.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=spikes.GetComponent<MeshRenderer>();mr.sharedMaterial=spikeMaterial;mr.sortingOrder=-3;
   }
   var background=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>()).First(r=>r.sprite);
   var back=new GameObject("原始数字背景",typeof(SpriteRenderer));back.transform.SetParent(root.transform,false);
   var sr=back.GetComponent<SpriteRenderer>();sr.sprite=background.sprite;sr.sharedMaterial=material;sr.sortingOrder=-10;
   back.transform.localPosition=new Vector3(w*.5f,h*.5f,0);back.transform.localScale=new Vector3(w/sr.sprite.bounds.size.x,h/sr.sprite.bounds.size.y,1);
   string prefabPath="Assets/Tide/Resources/Tide/Maps/Terrain"+id+".prefab";
   PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
   EditorSceneManager.CloseScene(source,true);
   if(!replaceScenes)continue;
   string scenePath="Assets/Scenes/Tide/Trial"+(i+1).ToString("00")+"_M"+id+".unity";
   var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
   foreach(var go in scene.GetRootGameObjects())if(go.name.StartsWith("原图块地形 ")||go.name=="花田残影"||go.name.StartsWith("原始地图 ")||(go.GetComponent<Grid>()&&go.GetComponentInChildren<Tilemap>()))UnityEngine.Object.DestroyImmediate(go);
   PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),scene);
   EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);
  }
  AssetDatabase.SaveAssets();return "Updated "+count+" map prefabs, tiles: "+total;
 }
 // Both ledges use tile faces and fill from this room's masonry layer.
 static void BuildOpeningExitLedges(Tilemap map){
  var rim=map.GetTile(new Vector3Int(2,1,0));
  var fill=map.GetTile(new Vector3Int(2,0,0));
  for(int x=1;x<=4;x++){
   for(int y=2;y<4;y++)map.SetTile(new Vector3Int(x,y,0),fill);
   map.SetTile(new Vector3Int(x,4,0),rim);
  }
  for(int x=20;x<=22;x++){
   map.SetTile(new Vector3Int(x,10,0),null);
   map.SetTile(new Vector3Int(x,9,0),rim);
  }

  // Raise the middle landing above the ordinary jump from the right ledge.
  // Keep the original masonry faces and sloped rim at the new height.
  CopyTile(map,new Vector3Int(12,10,0),new Vector3Int(12,13,0));
  for(int x=12;x<=15;x++){
   CopyTile(map,new Vector3Int(x,9,0),new Vector3Int(x,12,0));
   for(int y=9;y<=11;y++)CopyTile(map,new Vector3Int(x,8,0),new Vector3Int(x,y,0));
  }
 }
 static void CopyTile(Tilemap map,Vector3Int source,Vector3Int destination){
  map.SetTile(destination,map.GetTile(source));
  map.SetTileFlags(destination,TileFlags.None);
  map.SetTransformMatrix(destination,map.GetTransformMatrix(source));
  map.SetColor(destination,map.GetColor(source));
 }
 static void AddExitGate(GameObject root,Sprite spikeArt,List<Vector3> spikeVertices,List<Vector2> spikeUV,List<int> spikeTriangles){
    // The rail projects from the last hanging wall, above the red foothold and exit ledge.
    // It reuses the map's own tooth silhouettes and the same pixels for collision and art.
    const float left=26.68f,right=29.85f,baseY=15.75f;
    AddSpikeRun(root,spikeArt,new Rect(left,baseY,right-left,0),spikeVertices,spikeUV,spikeTriangles,true);
    var rail=new GameObject("出口墙沿刺座",typeof(SpriteRenderer),typeof(BoxCollider2D));
    rail.transform.SetParent(root.transform,false);
    rail.transform.localPosition=new Vector3((left+right)*.5f,baseY+.09f);
    var edge=rail.GetComponent<SpriteRenderer>();edge.sprite=TideArt.Pic("Art/PlatformNormal");
    rail.transform.localScale=new Vector3((right-left)/edge.sprite.bounds.size.x,.20f/edge.sprite.bounds.size.y,1);
    edge.color=new Color(.69f,.58f,.63f);edge.sortingOrder=-2;
    rail.GetComponent<BoxCollider2D>().size=edge.sprite.bounds.size;
   }
 // Use the eight intact middle teeth of the supplied strip, starting and ending in valleys.
 // Trim both artwork and collision at the masonry surface so no light pixels show through the brick.
 static void AddSpikeRun(GameObject root,Sprite art,Rect span,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,bool downward=false){
  const int teethPerSample=8;
  const float firstValley=510,toothPitch=169.5f,basePixel=956,topPixel=1195;
  int count=Mathf.Max(1,Mathf.RoundToInt(span.width*art.pixelsPerUnit/toothPitch));
  float toothWidth=span.width/count,bottom=span.y-.125f,top=span.y+.75f;
  float Orient(float y)=>downward ? 2f*span.y-y : y;
  var collider=new GameObject("尖刺轮廓").AddComponent<PolygonCollider2D>();
  collider.transform.SetParent(root.transform,false);
  var paths=new List<Vector2[]>();
  for(int first=0;first<count;first+=teethPerSample){
   int teeth=Mathf.Min(teethPerSample,count-first);
   float a=span.x+first*toothWidth,b=span.x+(first+teeth)*toothWidth;
   float lastValley=firstValley+teeth*toothPitch;
   float surfacePixel=Mathf.Lerp(basePixel,topPixel,(span.y-bottom)/(top-bottom));
   int n=vertices.Count;
   vertices.AddRange(new[]{new Vector3(a,span.y),new Vector3(a,Orient(top)),new Vector3(b,Orient(top)),new Vector3(b,span.y)});
   float u0=firstValley/art.texture.width,u1=lastValley/art.texture.width,v0=surfacePixel/art.texture.height,v1=topPixel/art.texture.height;
   uv.AddRange(new[]{new Vector2(u0,v0),new Vector2(u0,v1),new Vector2(u1,v1),new Vector2(u1,v0)});
   triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
   for(int shape=0;shape<art.GetPhysicsShapeCount();shape++){
    var points=new List<Vector2>();art.GetPhysicsShape(shape,points);
    for(int j=0;j<points.Count;j++)points[j]=points[j]*art.pixelsPerUnit+art.pivot+art.rect.position;
    points=ClipX(ClipX(points,firstValley,true),lastValley,false);
    // Clip the lower contour at the exact same crop as the material.
    for(int j=0;j<points.Count;j++)points[j]=new Vector2(points[j].y,points[j].x);
    points=ClipX(points,surfacePixel,true);
    for(int j=0;j<points.Count;j++)points[j]=new Vector2(Mathf.Lerp(a,b,(points[j].y-firstValley)/(lastValley-firstValley)),Orient(Mathf.Lerp(bottom,top,(points[j].x-basePixel)/(topPixel-basePixel))));
    if(points.Count>=3)paths.Add(points.ToArray());
   }
  }
  collider.pathCount=paths.Count;
  for(int i=0;i<paths.Count;i++)collider.SetPath(i,paths[i]);
 }
 static bool IsSpike(Sprite sprite){return sprite&&sprite.name.StartsWith("IMG_SEGMENT_20261115_232713");}
 static bool HasGround(Tilemap[] maps,Vector3Int cell){return maps.Any(map=>map.HasTile(cell)&&!IsSpike(map.GetSprite(cell)));}
 static System.Collections.Generic.List<Vector2> ClipX(System.Collections.Generic.List<Vector2> input,float edge,bool keepRight) {
  var result=new System.Collections.Generic.List<Vector2>();if(input.Count==0)return result;
  var a=input[input.Count-1];bool aInside=keepRight?a.x>=edge:a.x<=edge;
  foreach(var b in input) {
   bool bInside=keepRight?b.x>=edge:b.x<=edge;
   if(aInside!=bInside)result.Add(Vector2.Lerp(a,b,(edge-a.x)/(b.x-a.x)));
   if(bInside)result.Add(b);a=b;aInside=bInside;
  }
  return result;
 }
}
