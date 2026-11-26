using System;
using UnityEngine;

public static class TideCampaignProgress
{
    public sealed class Entry
    {
        public readonly string label;
        readonly string[] scenes;
        public string StartScene => scenes[0];
        public Entry(string label, params string[] scenes) { this.label=label; this.scenes=scenes; }
        public bool Contains(string scene) => Array.IndexOf(scenes, scene) >= 0;
    }

    // Keep these save indices stable. Story entries can span several playable scenes.
    static readonly Entry[] entries = {
        new Entry("第一幕","01_Cottage","02_Lighthouse","03_Meadow","04_Rift"),
        new Entry("第 1 关","Trial01_M0"), new Entry("第 2 关","Trial02_M01"),
        new Entry("第二幕","05_MemoryCottage","06_MemoryShore","07_LanternDeck"),
        new Entry("第 3 关","Trial03_M03"), new Entry("第 4 关","Trial04_M04"),
        new Entry("第三幕","08_CottagePromise","09_CottageLetter","10_ShoreReunion"),
        new Entry("第 5 关","Trial05_M05"), new Entry("第 6 关","Trial06_M2"),
        new Entry("第四幕","11_ParkPromise"),
        new Entry("第 7 关","Trial07_M3"), new Entry("第 8 关","Trial08_M4"),
        new Entry("第五幕","12_StreetOrigin"),
        new Entry("第 9 关","Trial09_M5"), new Entry("第 10 关","Trial10_M6"),
        new Entry("终幕","13_DataStation","14_CollapsingWorld","15_SunsetMeadow")
    };
    const string ReachedKey="Tide.Campaign.Reached";
    const string ResumeKey="Tide.Campaign.Resume";
    public const string LastTrialKey="Tide.LastVisitedTrial";
    public static int Count => entries.Length;
    public static Entry At(int index) => entries[index];

    static int Find(string scene)
    {
        for(int i=0;i<entries.Length;i++)
            if(entries[i].Contains(scene))return i;
        return -1;
    }

    static int Reached()
    {
        if(PlayerPrefs.HasKey(ReachedKey))return PlayerPrefs.GetInt(ReachedKey) | 1;
        // The old menu let players enter rooms out of order. An unlocked hint is also evidence of a visit.
        string last=PlayerPrefs.GetString(LastTrialKey);
        int lastIndex=Find(last);
        int reached=1;
        for(int i=0;i<entries.Length;i++)
        {
            string name=entries[i].StartScene;
            if(!name.StartsWith("Trial",StringComparison.Ordinal))continue;
            int room=int.Parse(name.Substring(5,2));
            if(i==lastIndex || TideGuideBook.Unlocked(room))reached|=1<<i;
        }
        PlayerPrefs.SetInt(ReachedKey,reached);
        if(lastIndex>=0 && last.StartsWith("Trial",StringComparison.Ordinal))PlayerPrefs.SetString(ResumeKey,last);
        PlayerPrefs.Save();
        return reached;
    }

    public static bool IsUnlocked(int index) => index>=0 && index<Count && (Reached() & (1<<index))!=0;
    public static bool HasSavedRun => PlayerPrefs.HasKey(ResumeKey);
    public static string ResumeScene
    {
        get {
            int reached=Reached();
            string scene=PlayerPrefs.GetString(ResumeKey);
            int index=Find(scene);
            return index>=0 && (reached & (1<<index))!=0 ? scene : entries[0].StartScene;
        }
    }

    public static void Arrive(string scene)
    {
        int index=Find(scene);
        if(index<0)return;
        int reached=Reached();
        if((reached & (1<<index))!=0 && PlayerPrefs.GetString(ResumeKey)==scene)return;
        PlayerPrefs.SetInt(ReachedKey,reached | (1<<index));
        PlayerPrefs.SetString(ResumeKey,scene);
        if(scene.StartsWith("Trial",StringComparison.Ordinal))PlayerPrefs.SetString(LastTrialKey,scene);
        PlayerPrefs.Save();
    }

    public static void FinishRun()
    {
        PlayerPrefs.DeleteKey(ResumeKey);
        PlayerPrefs.Save();
    }
}
