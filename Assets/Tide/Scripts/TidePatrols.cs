using System.Collections.Generic;
using UnityEngine;

// Endpoints are the green patrol marks in the planner's November 24 level photos,
// expressed in the returned levels' tile coordinates (one tile = one world unit).
public sealed class TidePatrols
{
    // Open Syobon's four-direction enemy moves 4 px per update, versus an 8 px run cap.
    const float Speed = TideTrial.RunSpeed * .5f;
    const float DrawingWidth = 1.58f;

    struct Route
    {
        public readonly int level;
        public readonly Vector2 start;
        public readonly Vector2 end;

        public Route(int level, float x1, float y1, float x2, float y2)
        {
            this.level = level;
            start = new Vector2(x1, y1);
            end = new Vector2(x2, y2);
        }
    }

    static readonly Route[] routes = {
        new Route(3, 18.8f, 17.45f, 18.8f, 3.95f),     // 576
        new Route(6, 1.5f, 14.35f, 28.6f, 14.35f),    // 568
        new Route(7, 26.9f, 15.3f, 26.9f, 2.2f),      // 570
        new Route(8, 26.25f, 15.15f, 26.25f, 3.1f),   // 572
        new Route(9, 2.45f, 13.65f, 18.15f, 13.65f),  // 573, upper passage
        new Route(9, 28.3f, 15.3f, 28.3f, 4.1f),      // 573, right shaft
        // Guard the far end of the last green foothold. The approach and its
        // left landing edge must stay open for the dash from the purple pad.
        new Route(9, 28.75f, 5.05f, 30.05f, 5.05f),
        new Route(10, 10f, 6.2f, 26.9f, 6.2f)         // 575
    };

    sealed class Patrol
    {
        internal readonly Vector2 start;
        internal readonly Vector2 direction;
        internal readonly float distance;
        internal readonly Transform visual;

        internal Patrol(Route route, Transform visual)
        {
            this.visual = visual;
            start = route.start;
            distance = Vector2.Distance(route.start, route.end);
            direction = (route.end - route.start) / distance;
            visual.position = start;
        }
    }

    readonly List<Patrol> active = new List<Patrol>();
    Vector2 hitExtents;
    float travelled;
    public float Travelled => travelled;

    public static TidePatrols Create(int number, Transform parent)
    {
        TidePatrols group = null;
        Sprite sprite = null;
        foreach (var route in routes)
        {
            if (route.level != number) continue;
            if (group == null)
            {
                sprite = TideArt.Pic("Art/DataErrorPatrol");
                if (!sprite)
                {
                    Debug.LogError("缺少巡游怪物原画 DataErrorPatrol。", parent);
                    return null;
                }
                group = new TidePatrols();
                // Inset the hitbox slightly from the visible border of the error window.
                float drawingHeight = DrawingWidth * sprite.rect.height / sprite.rect.width;
                group.hitExtents = new Vector2(DrawingWidth * .45f, drawingHeight * .44f);
            }
            var renderer = new GameObject("数据损坏巡游怪 " + (group.active.Count + 1)).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = 8;
            renderer.transform.localScale = Vector3.one * (DrawingWidth / (sprite.rect.width / sprite.pixelsPerUnit));
            group.active.Add(new Patrol(route, renderer.transform));
        }
        return group;
    }

    public void Reset() => RestoreRetryDistance(0);

    public void RestoreRetryDistance(float distance)
    {
        travelled = distance;
        foreach (var patrol in active) patrol.visual.position = PositionAt(patrol, travelled);
    }

    public bool ThreatensLanding(Vector2 feet, float halfWidth, float height, float time)
    {
        Vector2 center = feet + Vector2.up * (height * .5f);
        // Include the patrol's full travel distance so turning near an endpoint is covered.
        Vector2 half = hitExtents + new Vector2(halfWidth, height * .5f);
        foreach (var patrol in active)
        {
            Vector2 margin = patrol.direction * (Speed * time);
            Vector2 limit = half + new Vector2(Mathf.Abs(margin.x), Mathf.Abs(margin.y));
            Vector2 offset = center - (Vector2)patrol.visual.position;
            if (Mathf.Abs(offset.x) <= limit.x && Mathf.Abs(offset.y) <= limit.y) return true;
        }
        return false;
    }

    public bool Tick(float dt, Vector2 previousFeet, Vector2 currentFeet, float heroHalfWidth, float heroHeight)
    {
        travelled += Speed * dt;
        Vector2 bodyOffset = Vector2.up * (heroHeight * .5f);
        Vector2 half = hitExtents + new Vector2(heroHalfWidth, bodyOffset.y);
        bool contact = false;
        foreach (var patrol in active)
        {
            Vector2 before = patrol.visual.position;
            Vector2 after = PositionAt(patrol, travelled);
            patrol.visual.position = after;
            contact |= CrossesBox(previousFeet + bodyOffset - before, currentFeet + bodyOffset - after, half);
        }
        return contact;
    }

    static Vector2 PositionAt(Patrol patrol, float distance) =>
        patrol.start + patrol.direction * Mathf.PingPong(distance, patrol.distance);

    // Sweep the relative movement, so a dash or two actors crossing cannot skip a hit.
    static bool CrossesBox(Vector2 from, Vector2 to, Vector2 half)
    {
        float enter = 0f, leave = 1f;
        return ClipAxis(from.x, to.x - from.x, half.x, ref enter, ref leave)
            && ClipAxis(from.y, to.y - from.y, half.y, ref enter, ref leave);
    }

    static bool ClipAxis(float origin, float delta, float extent, ref float enter, ref float leave)
    {
        if (Mathf.Abs(delta) < .00001f) return Mathf.Abs(origin) <= extent;
        float a = (-extent - origin) / delta;
        float b = (extent - origin) / delta;
        enter = Mathf.Max(enter, Mathf.Min(a, b));
        leave = Mathf.Min(leave, Mathf.Max(a, b));
        return enter <= leave;
    }
}

