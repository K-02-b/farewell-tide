using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Space is a dialogue key in story scenes. Unity also maps it to UI Submit,
// which would otherwise activate the last clicked button, including a choice.
public sealed class TideNarrativeButton : Button
{
    public override void OnSubmit(BaseEventData data)
    {
        if (!TidePause.BlocksInput && !Input.GetKeyDown(KeyCode.Space)) base.OnSubmit(data);
    }
}
