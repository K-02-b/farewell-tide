using System;
using UnityEngine;
using UnityEngine.UI;

// The pause menu keeps unlocked diagrams available when a player revisits a room.
public sealed class TideGuideBook
{
    const int Rooms = 10;
    readonly TideHUD hud;
    readonly Image[] labels = new Image[Rooms];
    readonly Text title, note, lockedMessage;
    readonly Image diagram;
    public readonly RectTransform panel;

    static string Key(int room) => "Tide.RouteHint.Unlocked." + room;
    public static bool Unlocked(int room) => PlayerPrefs.GetInt(Key(room), 0) == 1;
    public static void Unlock(int room)
    {
        if (Unlocked(room)) return;
        PlayerPrefs.SetInt(Key(room), 1);
        PlayerPrefs.Save();
    }

    public TideGuideBook(TideHUD hud, RectTransform parent, int room, Action close)
    {
        this.hud = hud;
        panel = hud.Box("关卡提示册", parent, Vector2.one * .5f, Vector2.zero, new Vector2(1050, 610), TideHUD.ink);
        float fit = Mathf.Min(1, parent.rect.width / 1080f, parent.rect.height / 630f);
        if (fit > 0) panel.localScale = Vector3.one * fit;
        title = hud.Caption(Box("页标题", 60, 266, 720, 42), "", 26, TextAnchor.MiddleCenter);
        hud.Caption(Box("目录", -427, 265, 160, 42), "关卡提示", 24, TextAnchor.MiddleCenter);
        for (int i = 0; i < Rooms; i++)
        {
            int target = i + 1;
            var tab = Box("关卡 " + target, -427, 211 - i * 43, 148, 36);
            labels[i] = tab.GetComponent<Image>();
            hud.Caption(tab, "第 " + target + " 关", 18, TextAnchor.MiddleCenter);
            hud.Click(tab, () => Show(target));
        }
        var frame = Box("路线图底板", 77, 25, 794, 449);
        frame.GetComponent<Image>().color = new Color32(16, 19, 26, 255);
        diagram = Box("路线图", 77, 25, 788, 443).GetComponent<Image>();
        diagram.preserveAspect = true;
        lockedMessage = hud.Caption(Box("解锁条件", 77, 25, 656, 60), "在这一关失败三次后，放置路线会出现在这里。", 21, TextAnchor.MiddleCenter);
        note = hud.Caption(Box("图注", 77, -238, 818, 66), "", 18, TextAnchor.MiddleCenter);
        var back = Box("返回选关", -427, -248, 148, 40);
        back.GetComponent<Image>().color = new Color32(78, 62, 43, 255);
        hud.Caption(back, "返回", 19, TextAnchor.MiddleCenter);
        hud.Click(back, () => close());
        Show(Mathf.Clamp(room, 1, Rooms));
    }

    RectTransform Box(string name, float x, float y, float width, float height)
    {
        return hud.Box(name, panel, Vector2.one * .5f, new Vector2(x, y), new Vector2(width, height), Color.clear);
    }

    void Show(int room)
    {
        for (int i = 0; i < Rooms; i++)
            labels[i].color = i + 1 == room ? new Color32(111, 79, 46, 255) : new Color32(57, 51, 47, 255);
        title.text = "第 " + room + " 关";
        bool open = Unlocked(room);
        diagram.sprite = open ? TideArt.Pic("Guides/Room" + room.ToString("00")) : null;
        diagram.enabled = diagram.sprite != null;
        diagram.color = Color.white;
        lockedMessage.gameObject.SetActive(!open);
        note.text = open ? "按编号查看参考落点。红台落脚，绿台高跳，紫台冲刺。" : "";
        if (open && room == 5) note.text = "6 号红台下的刺会先抖动。趁它还没弹起，向右短跳，从倒刺下方落到门前。";
        if (open && room == 9) note.text = "在 3 号紫台朝右按 V，冲向 4 号绿台。落在绿台左端后，看准右侧巡逻再起跳。";
    }
}
