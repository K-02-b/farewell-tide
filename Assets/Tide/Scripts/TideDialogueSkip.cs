using UnityEngine;

internal sealed class TideDialogueSkip
{
    const double HoldSeconds = 2.0;
    const double RepeatSeconds = 0.15;
    double nextAdvance = double.PositiveInfinity;

    public void Reset()
    {
        nextAdvance = double.PositiveInfinity;
    }

    // Keep an existing hold through staging between lines. Only visible dialogue can start it or advance.
    public bool ShouldRepeat(bool dialogueVisible, bool betweenLines = false)
    {
        if (!Input.GetKey(KeyCode.Space) || (!dialogueVisible && !betweenLines))
            Reset();
        else if (dialogueVisible)
        {
            double now = Time.unscaledTimeAsDouble;
            if (Input.GetKeyDown(KeyCode.Space))
                nextAdvance = now + HoldSeconds;
            else if (now >= nextAdvance)
            {
                nextAdvance = now + RepeatSeconds;
                return true;
            }
        }
        return false;
    }
}
