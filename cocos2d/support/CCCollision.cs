using System;
using System.Collections.Generic;

namespace Cocos2D;

/// <summary>
/// Static utility class for common 2D collision detection operations.
/// Built on top of CCRect.IntersectsRect() and CCRect.ContainsPoint().
/// </summary>
public static class CCCollision
{
    /// <summary>
    /// Checks whether two rectangles overlap (AABB intersection test).
    /// </summary>
    public static bool Overlaps(CCRect a, CCRect b)
    {
        return a.IntersectsRect(b);
    }

    /// <summary>
    /// Checks whether two nodes' bounding boxes overlap, with an optional shrink factor.
    /// A shrink of 1.0 uses full bounding box, 0.5 uses half-size, etc.
    /// </summary>
    public static bool Overlaps(CCNode a, CCNode b, float shrink = 1f)
    {
        var boxA = GetShrunkBounds(a, shrink);
        var boxB = GetShrunkBounds(b, shrink);
        return boxA.IntersectsRect(boxB);
    }

    /// <summary>
    /// Returns a node's bounding box shrunk by the given factor.
    /// A factor of 1.0 returns the full bounding box.
    /// A factor of 0.5 returns a box that is 50% the original size, centered.
    /// </summary>
    public static CCRect GetShrunkBounds(CCNode node, float shrinkFactor)
    {
        var box = node.BoundingBox;
        shrinkFactor = Math.Max(0f, Math.Min(1f, shrinkFactor));
        if (shrinkFactor >= 1f)
            return box;

        float shrinkX = box.Size.Width * (1f - shrinkFactor) * 0.5f;
        float shrinkY = box.Size.Height * (1f - shrinkFactor) * 0.5f;
        return new CCRect(
            box.Origin.X + shrinkX,
            box.Origin.Y + shrinkY,
            box.Size.Width * shrinkFactor,
            box.Size.Height * shrinkFactor);
    }

    /// <summary>
    /// Checks whether two nodes should test for collision based on their
    /// CollisionCategory and CollisionCategoryMask bitmasks.
    /// Returns true only if both nodes' categories match the other's mask (bilateral).
    /// </summary>
    public static bool ShouldCollide(CCNode a, CCNode b)
    {
        return (a.CollisionCategory & b.CollisionCategoryMask) != 0
            && (b.CollisionCategory & a.CollisionCategoryMask) != 0;
    }

    /// <summary>
    /// Checks a single node against a list of nodes for collisions.
    /// Calls the handler for each collision found.
    /// </summary>
    public static void CheckCollisions<T>(CCNode single, IList<T> group, float shrink, Action<CCNode, T> onCollision) where T : CCNode
    {
        var singleBox = GetShrunkBounds(single, shrink);
        for (int i = group.Count - 1; i >= 0; i--)
        {
            var other = group[i];
            if (!other.Visible) continue;

            var otherBox = GetShrunkBounds(other, shrink);
            if (singleBox.IntersectsRect(otherBox))
            {
                onCollision(single, other);
            }
        }
    }

    /// <summary>
    /// Checks a single node against a list of nodes for collisions,
    /// respecting CollisionCategory/CollisionCategoryMask filtering.
    /// Only tests pairs where ShouldCollide returns true.
    /// </summary>
    public static void CheckCollisionsFiltered<T>(CCNode single, IList<T> group, float shrink, Action<CCNode, T> onCollision) where T : CCNode
    {
        if (!single.Visible) return;

        var singleBox = GetShrunkBounds(single, shrink);
        for (int i = group.Count - 1; i >= 0; i--)
        {
            var other = group[i];
            if (!other.Visible) continue;
            if (!ShouldCollide(single, other)) continue;

            var otherBox = GetShrunkBounds(other, shrink);
            if (singleBox.IntersectsRect(otherBox))
            {
                onCollision(single, other);
            }
        }
    }

    /// <summary>
    /// Checks two groups of nodes against each other for collisions.
    /// Calls the handler for each collision found.
    /// </summary>
    public static void CheckGroupCollisions<TA, TB>(IList<TA> groupA, IList<TB> groupB, float shrink, Action<TA, TB> onCollision)
        where TA : CCNode
        where TB : CCNode
    {
        for (int i = groupA.Count - 1; i >= 0; i--)
        {
            var a = groupA[i];
            if (!a.Visible) continue;

            var boxA = GetShrunkBounds(a, shrink);

            for (int j = groupB.Count - 1; j >= 0; j--)
            {
                var b = groupB[j];
                if (!b.Visible) continue;

                var boxB = GetShrunkBounds(b, shrink);
                if (boxA.IntersectsRect(boxB))
                {
                    onCollision(a, b);
                }
            }
        }
    }

    /// <summary>
    /// Checks two groups of nodes against each other for collisions,
    /// respecting CollisionCategory/CollisionCategoryMask filtering.
    /// Only tests pairs where ShouldCollide returns true.
    /// </summary>
    public static void CheckGroupCollisionsFiltered<TA, TB>(IList<TA> groupA, IList<TB> groupB, float shrink, Action<TA, TB> onCollision)
        where TA : CCNode
        where TB : CCNode
    {
        for (int i = groupA.Count - 1; i >= 0; i--)
        {
            var a = groupA[i];
            if (!a.Visible) continue;

            var boxA = GetShrunkBounds(a, shrink);

            for (int j = groupB.Count - 1; j >= 0; j--)
            {
                var b = groupB[j];
                if (!b.Visible) continue;
                if (!ShouldCollide(a, b)) continue;

                var boxB = GetShrunkBounds(b, shrink);
                if (boxA.IntersectsRect(boxB))
                {
                    onCollision(a, b);
                }
            }
        }
    }

    /// <summary>
    /// Checks whether a point is within a node's bounding box (with optional shrink).
    /// </summary>
    public static bool ContainsPoint(CCNode node, CCPoint point, float shrink = 1f)
    {
        var box = GetShrunkBounds(node, shrink);
        return box.ContainsPoint(point);
    }
}
