using UnityEngine;

public sealed class TideTrialFragment
{
    readonly SpriteRenderer pickup;
    readonly TextAsset document;

    public TideTrialFragment(Transform owner,Vector2 position,TextAsset document)
    {
        this.document=document;
        pickup=TideArt.Place(document.name,TideArt.Pic("Art/LetterBook"),position.x,position.y,.64f,7);
        pickup.transform.SetParent(owner,true);
        pickup.enabled=!TideJournal.Has(document);
    }

    public bool Touch(Vector2 feet,float halfWidth,float height)
    {
        if(!pickup.enabled)return false;
        var character=new Bounds(feet+Vector2.up*(height*.5f),new Vector3(2*halfWidth,height,.1f));
        if(!character.Intersects(pickup.bounds))return false;
        TideJournal.Collect(document);
        pickup.enabled=false;
        return true;
    }
}

