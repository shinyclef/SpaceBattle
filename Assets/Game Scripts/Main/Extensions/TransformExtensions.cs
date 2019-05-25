using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// Look at target excluding the y axis. Overload for Transform parameter.
    /// </summary>
    public static void LookAtNoY(this Transform transform, Transform target)
    {
        LookAtNoY(transform, target.position);
    }

    /// <summary>
    /// Look at target excluding the y axis.
    /// </summary>
    public static void LookAtNoY(this Transform transform, Vector3 targetPos)
    {
        targetPos.y = transform.position.y;
        transform.LookAt(targetPos);
    }

    /// <summary>
    /// Get position from transform plus additional y offset.
    /// </summary>
    /// <returns></returns>
    public static Vector3 PositionWithYOffset(this Transform transform, float yOffset)
    {
        Vector3 pos = transform.position;
        pos.y += yOffset;
        return pos;
    }

    /// <summary>
    /// Gets the relative Vector3 of a position in relation to the origin, taking rotation into account.
    /// </summary>
    public static Vector3 GetRelativePosition(this Transform transform, Vector3 position)
    {
        Vector3 distance = position - transform.position;
        Vector3 relativePosition = Vector3.zero;
        relativePosition.x = Vector3.Dot(distance, transform.right.normalized);
        relativePosition.y = Vector3.Dot(distance, transform.up.normalized);
        relativePosition.z = Vector3.Dot(distance, transform.forward.normalized);
        return relativePosition;
    }

    /// <summary>
	/// Sets a transforms position and rotation to zero / identity.
	/// </summary>
	public static void ResetPositionAndRotation(this Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Scales a transform around a given pivot point. This method expects that scales are uniform (x == y == z);
    /// </summary>
    public static void ScaleAround(this Transform t, float newSize, Vector3 worldPivot)
    {
        Vector3 startScale = t.localScale;
        Vector3 pivotDelta = t.position - worldPivot;

        float oldSize = startScale.x;
        float relativeScale = newSize / oldSize;
        Vector3 finalPos = (pivotDelta * relativeScale) + worldPivot;

        t.localScale = new Vector3(newSize, newSize, newSize);
        t.position = finalPos;
    }

    /// <summary>
    /// Updates the pivot point of a rect transform without altering its position.
    /// </summary>
    public static void SetPivot(this RectTransform rectTransform, Vector2 pivot)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 size = rectTransform.rect.size;
        Vector3 scale = rectTransform.localScale;
        Vector2 deltaPivot = rectTransform.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x * scale.x, deltaPivot.y * size.y * scale.y);
        rectTransform.pivot = pivot;
        rectTransform.localPosition -= deltaPosition;
    }
}