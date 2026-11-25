using System.Collections.Generic;
using UnityEngine;

public sealed class TideFlyingSpikes
{
    // One point inside each marked strip, in tile coordinates of the returned map.
    static readonly Vector2[][] marks = {
        new Vector2[0], new[] { new Vector2(19.5f, 5.3f) },
        new[] { new Vector2(17.5f, 3.3f) },
        new[] { new Vector2(24.5f, 4.3f), new Vector2(28f, 6.3f) },
        new[] { new Vector2(13f, 9.3f) },
        new[] { new Vector2(27.5f, 8.3f) },
        new[] { new Vector2(9.5f, 11.3f), new Vector2(22.5f, 8.3f) },
        new[] { new Vector2(21.5f, 2.3f) }, new Vector2[0], new Vector2[0],
        new[] { new Vector2(15f, 1.3f) }
    };

    // Aim once on entering the strip. Early reversals can clear it before the teeth reach the player.
    const float InterceptTime = .25f;
    readonly float minimumSpeed;
    readonly List<Strip> strips = new List<Strip>();
    sealed class Strip
    {
        public Bounds bounds;
        public PolygonCollider2D source;
        public Vector2[][] outlines;
        public GameObject visual;
        public Mesh mesh;
        public float lift, speed, warningLeft;
        public bool warnsOnLanding;
        public bool fired;
    }

    public TideFlyingSpikes(int level, IEnumerable<Collider2D> colliders, Transform parent, float jumpSpeed)
    {
        // Keep teeth moving out of the lane even on a very low or descending approach.
        minimumSpeed = jumpSpeed * (8f / 12f) * .25f;
        if (level < 1 || level >= marks.Length) return;
        foreach (Vector2 mark in marks[level])
        {
            Strip strip = null;
            foreach (var collider in colliders)
            {
                var polygon = collider as PolygonCollider2D;
                if (!polygon || !polygon.bounds.Contains(new Vector3(mark.x, mark.y, polygon.bounds.center.z))) continue;
                strip = CreateStrip(polygon);
                break;
            }
            if (strip != null) { strip.warnsOnLanding = level == 5; strips.Add(strip); }
            else Debug.LogError("第 " + level + " 关的飞刺未绑定到图块 " + mark, parent);
        }
    }

    static Strip CreateStrip(PolygonCollider2D polygon)
    {
        var original = polygon.transform.parent.Find("边缘裁切尖刺");
        if (!original) return null;
        var shared = original.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = shared.vertices;
        int[] triangles = shared.triangles;
        var selected = new List<int>();
        Bounds bounds = polygon.bounds;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 center = original.TransformPoint((vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f);
            if (center.x < bounds.min.x || center.x > bounds.max.x || center.y < bounds.min.y || center.y > bounds.max.y) continue;
            selected.Add(triangles[i]);
            selected.Add(triangles[i + 1]);
            selected.Add(triangles[i + 2]);
        }
        if (selected.Count == 0) return null;
        // Use the exact same UVs and material as the stationary teeth.
        var mesh = new Mesh { name = "飞刺原图切片", vertices = vertices, uv = shared.uv };
        mesh.SetTriangles(selected, 0);
        var visual = new GameObject("上升尖刺", typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(original, false);
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = visual.GetComponent<MeshRenderer>();
        var sourceRenderer = original.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder + 1;
        visual.SetActive(false);
        var outlines = new Vector2[polygon.pathCount][];
        for (int i = 0; i < outlines.Length; i++)
        {
            var path = polygon.GetPath(i);
            for (int j = 0; j < path.Length; j++) path[j] = polygon.transform.TransformPoint(path[j] + polygon.offset);
            outlines[i] = path;
        }
        return new Strip { bounds = bounds, source = polygon, outlines = outlines, visual = visual, mesh = mesh };
    }

    public void Reset()
    {
        foreach (var strip in strips)
        {
            strip.fired = false;
            strip.lift = strip.speed = strip.warningLeft = 0;
            strip.visual.transform.localPosition = Vector3.zero;
            strip.visual.SetActive(false);
        }
    }

    public void Dispose()
    {
        foreach (var strip in strips) Object.Destroy(strip.mesh);
    }

    // Intersect the foot movement with the region above this strip, including a toe at its edge.
    static bool EntersTrigger(Bounds b, Vector2 before, Vector2 after, float half, out float enter)
    {
        enter = 0f;
        float left = b.min.x - half, right = b.max.x + half, top = b.max.y;
        if (Mathf.Max(before.x, after.x) < left || Mathf.Min(before.x, after.x) > right ||
            Mathf.Max(before.y, after.y) < top) return false;
        if (before.x < left) enter = (left - before.x) / (after.x - before.x);
        else if (before.x > right) enter = (right - before.x) / (after.x - before.x);
        if (before.y < top) enter = Mathf.Max(enter, (top - before.y) / (after.y - before.y));
        Vector2 at = Vector2.LerpUnclamped(before, after, enter);
        return at.x >= left - .0001f && at.x <= right + .0001f && at.y >= top - .0001f;
    }

    public bool Tick(Vector2 before, Vector2 feet, float half, float height, float dt)
    {
        if (dt <= 0f) return false;
        bool hit = false;
        foreach (var strip in strips)
        {
            Bounds b = strip.bounds;
            Vector2 start = before;
            float travelTime = dt;
            if (!strip.fired)
            {
                // The last red foothold in map 05 is approached from above. Arm near the
                // foothold, so passing overhead cannot spend its warning before landing.
                if (strip.warnsOnLanding && Mathf.Min(before.y, feet.y) > b.max.y + 2.1f) continue;
                if (!EntersTrigger(b, before, feet, half, out float enter)) continue;
                start = Vector2.LerpUnclamped(before, feet, enter);
                float gap = Mathf.Max(0f, start.y - b.max.y);
                float verticalSpeed = (feet.y - before.y) / dt;
                // An ascending jump slows after launch. A descending player is already closing the gap.
                float lead = verticalSpeed > 0f ? verticalSpeed * .7f : verticalSpeed;
                strip.speed = Mathf.Max(minimumSpeed, gap / InterceptTime + lead);
                travelTime = dt * (1f - enter);
                strip.fired = true;
                strip.warningLeft = strip.warnsOnLanding ? .60f : 0f;
            }
            if (strip.warningLeft > 0f)
            {
                float warning = Mathf.Min(travelTime, strip.warningLeft);
                strip.warningLeft -= warning;
                travelTime -= warning;
                strip.visual.SetActive(true);
                float tremble = .18f * Mathf.Abs(Mathf.Sin(strip.warningLeft * 24f));
                strip.visual.transform.localPosition = Vector3.up * tremble;
                if (travelTime <= 0f) continue;
                start = Vector2.Lerp(start, feet, warning / (warning + travelTime));
            }
            if (strip.lift >= 24f) continue;
            float lastLift = strip.lift;
            strip.lift = Mathf.Min(24f, lastLift + strip.speed * travelTime);
            strip.visual.SetActive(strip.lift < 24f);
            strip.visual.transform.position = strip.visual.transform.parent.position + Vector3.up * strip.lift;
            // Sweep the player's box in the source polygon's frame, including the strip's movement.
            Vector2 a = start - Vector2.up * lastLift, c = feet - Vector2.up * strip.lift;
            Rect box = Rect.MinMaxRect(Mathf.Min(a.x,c.x)-half, Mathf.Min(a.y,c.y),
                Mathf.Max(a.x,c.x)+half, Mathf.Max(a.y,c.y)+height);
            if (box.xMax < b.min.x || box.xMin > b.max.x || box.yMax < b.min.y || box.yMin > b.max.y) continue;
            if (Touches(box, strip)) hit = true;
        }
        return hit;
    }

    static bool Touches(Rect box, Strip strip)
    {
        if (strip.source.OverlapPoint(box.center)) return true;
        foreach (var path in strip.outlines)
            for (int i = 0, j = path.Length - 1; i < path.Length; j = i++)
            {
                Vector2 a = path[j], b = path[i];
                if (Mathf.Max(a.x,b.x) < box.xMin || Mathf.Min(a.x,b.x) > box.xMax ||
                    Mathf.Max(a.y,b.y) < box.yMin || Mathf.Min(a.y,b.y) > box.yMax) continue;
                // With overlap on both box axes already known, the edge normal is the last separating axis.
                Vector2 n = new Vector2(a.y - b.y, b.x - a.x);
                float radius = .5f * (Mathf.Abs(n.x) * box.width + Mathf.Abs(n.y) * box.height);
                if (Mathf.Abs(Vector2.Dot(n, a-box.center)) <= radius) return true;
            }
        return false;
    }
}
