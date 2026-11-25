using UnityEngine;

// Keep the playable camera and its screen/world mapping inside the original
// composition. Only the small leftover strips sample the outer pixels.
[DefaultExecutionOrder(900)]
[DisallowMultipleComponent]
public sealed class TideViewportEdges : MonoBehaviour
{
    Camera view, low, high;
    void Awake(){view=GetComponent<Camera>();}

    Camera EdgeCamera(string label)
    {
        var obj=new GameObject(label);
        obj.transform.SetParent(transform,false);
        var cam=obj.AddComponent<Camera>();
        cam.enabled=false;
        cam.orthographic=true;
        cam.clearFlags=CameraClearFlags.SolidColor;
        cam.useOcclusionCulling=false;
        cam.allowHDR=false;
        return cam;
    }

    void LateUpdate()
    {
        Rect r=view.rect;
        bool sides=r.width<.99999f;
        if(!view.enabled || (!sides && r.height>=.99999f)){Disable();return;}
        if(!low){low=EdgeCamera("画幅起边");high=EdgeCamera("画幅末边");}
        float y=view.orthographicSize, x=y*view.aspect;
        float sample=2*y/Mathf.Max(1,view.pixelHeight);
        if(sides)
        {
            Set(low,new Rect(0,0,r.xMin,1),-x,-x+sample,-y,y);
            Set(high,new Rect(r.xMax,0,1-r.xMax,1),x-sample,x,-y,y);
        }
        else
        {
            Set(low,new Rect(0,0,1,r.yMin),-x,x,-y,-y+sample);
            Set(high,new Rect(0,r.yMax,1,1-r.yMax),-x,x,y-sample,y);
        }
    }

    void Set(Camera cam,Rect rect,float left,float right,float bottom,float top)
    {
        cam.enabled=rect.width>.00001f && rect.height>.00001f;
        if(!cam.enabled)return;
        cam.targetTexture=view.targetTexture;
        cam.rect=rect;
        cam.depth=view.depth-1;
        cam.backgroundColor=view.backgroundColor;
        cam.cullingMask=view.cullingMask;
        cam.nearClipPlane=view.nearClipPlane;
        cam.farClipPlane=view.farClipPlane;
        cam.projectionMatrix=Matrix4x4.Ortho(left,right,bottom,top,cam.nearClipPlane,cam.farClipPlane);
    }
    void Disable(){if(low)low.enabled=false;if(high)high.enabled=false;}
    void OnDisable(){Disable();}
}
