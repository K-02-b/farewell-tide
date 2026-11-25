using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TideSceneJumpWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Tools/跳转场景")]
    public static void OpenWindow()
    {
        var window = GetWindow<TideSceneJumpWindow>("跳转场景");
        window.minSize = new Vector2(270, 280);
    }

    private void OnHierarchyChange() => Repaint();

    private void OnGUI()
    {
        var current = SceneManager.GetActiveScene();
        EditorGUILayout.LabelField("当前场景", LabelFor(current.name));
        scroll = EditorGUILayout.BeginScrollView(scroll);
        bool transitioning = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        using (new EditorGUI.DisabledScope(transitioning || EditorApplication.isCompiling))
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || !scene.path.StartsWith("Assets/Scenes/Tide/", System.StringComparison.Ordinal))
                    continue;

                string name = Path.GetFileNameWithoutExtension(scene.path);
                using (new EditorGUI.DisabledScope(scene.path == current.path))
                {
                    if (GUILayout.Button(new GUIContent(LabelFor(name), scene.path), GUILayout.Height(24)))
                    {
                        if (EditorApplication.isPlaying)
                            SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath(scene.path));
                        else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(scene.path);

                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static string LabelFor(string name)
    {
        switch (name)
        {
            case TideEnding.Station: return "结尾一　数据站";
            case TideEnding.Ruins: return "结尾一　崩坏世界";
            case TideEnding.Meadow: return "结尾一　花海重逢";
            case "00_Title": return "开始界面";
            case "01_Cottage": return "第一幕　灯塔小屋";
            case "02_Lighthouse": return "第一幕　灯塔外";
            case "03_Meadow": return "第一幕　草地";
            case "04_Rift": return "第一幕　裂隙";
            case "05_MemoryCottage": return "第二幕　小屋旧记忆";
            case "06_MemoryShore": return "第二幕　灯塔外的约定";
            case "12_StreetOrigin": return "第六幕　街边初识";
            case "11_ParkPromise": return "第五幕　公园里的约定";
            case "10_ShoreReunion": return "第四幕　屋外重逢";
            case "09_CottageLetter": return "第四幕　桌上的信";
            case "08_CottagePromise": return "第三幕　屋内誓言";
            case "07_LanternDeck": return "第二幕　灯塔顶端";
        }

        int map = name.IndexOf("_M", System.StringComparison.Ordinal);
        if (name.StartsWith("Trial", System.StringComparison.Ordinal) && map > 5 &&
            int.TryParse(name.Substring(5, map - 5), out int number))
            return $"第 {number} 关　地图 {name.Substring(map + 2)}";

        return name;
    }
}
