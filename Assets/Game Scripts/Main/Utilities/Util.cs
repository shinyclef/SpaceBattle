using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

public static class Util
{
    public static void SetHighThreadPriority()
    {
#if UNITY_WEBPLAYER
#else
        using (Process p = Process.GetCurrentProcess())
        {
            p.PriorityClass = ProcessPriorityClass.High;
        }
#endif
    }

    public static string TimeString()
    {
        return $"[{Time.realtimeSinceStartup} - Frame {Time.frameCount}]";
    }

    /// <summary>
    /// Dot product can be used to check how directly an object is in front/behind another object.
    /// 1 = in front. -1 behind. 0 = to either side.
    /// </summary>
    public static float GetDotProductFromSightConeDegrees(float sightConeDegrees)
    {
        return 1 - sightConeDegrees * 0.5f / 90;
    }

    /// <summary>
    /// Checks if the target obj position is within the viewer's LoS. LoS cone is defined by a dot product relationship.
    /// </summary>
    public static bool IsInLoSCone(Transform viewer, Vector3 targetObjPos, float minDotProduct)
    {
        Vector3 relativePos = (targetObjPos - viewer.position).normalized;
        float dot = Vector3.Dot(relativePos, viewer.transform.forward);
        return dot >= minDotProduct;
    }

    /// <summary>
    /// Checks if the target obj position is within the viewer's LoS. LoS cone is defined by a dot product relationship.
    /// </summary>
    public static bool IsInLoSCone(Vector3 viewer, Vector3 viewerForward, Vector3 targetObjPos, float minDotProduct)
    {
        Vector3 relativePos = (targetObjPos - viewer).normalized;
        float dot = Vector3.Dot(relativePos, viewerForward);
        return dot >= minDotProduct;
    }

    /// <summary>
    /// Check if there is line of sight between transform and target where target is the root object, but hit object doesn't have to be the root object.
    /// </summary>
    public static bool HasLineOfSightToRoot(Transform fromTransform, Transform toTransform, Transform hitTargetRoot, float distance, int layerMask)
    {
        RaycastHit hitInfo = new RaycastHit();
        Ray ray = new Ray(fromTransform.position, toTransform.position - fromTransform.position);
        bool hit = Physics.Raycast(ray, out hitInfo, distance, layerMask);
        //Debug.DrawRay(fromTransform.position, toTransform.position - fromTransform.position, Color.yellow);
        return hit && hitInfo.transform.root == hitTargetRoot;
    }

    /// <summary>
    /// Check if there is line of sight between transform and target directly.
    /// </summary>
    public static bool HasLineOfSightDirect(Transform fromTransform, Transform toTransform, float distance, int layerMask)
    {
        RaycastHit hitInfo = new RaycastHit();
        Ray ray = new Ray(fromTransform.position, toTransform.position - fromTransform.position);
        bool hit = Physics.Raycast(ray, out hitInfo, distance, layerMask);
        //Debug.DrawRay(fromTransform.position, toTransform.position - fromTransform.position, Color.yellow);
        return hit && hitInfo.transform == toTransform;
    }

    /// <summary>
    /// Checks if there is line of sight between from and to vectors3 with nothing from layerMask blocking the way.
    /// </summary>
    public static bool HasLineOfSightBetweenVectors(Vector3 from, Vector3 to, int layerMask)
    {
        Ray ray = new Ray(from, to - from);
        //Debug.DrawRay(from, to - from, Color.yellow);
        return !Physics.Raycast(ray, Vector3.Distance(from, to), layerMask);
    }

    /// <summary>
    /// Check if a layer is included in a layermask.
    /// </summary>
    public static bool LayerIsInLayerMask(int layer, LayerMask layermask)
    {
        if (((1 << layer) & layermask) != 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the closest position on a line from Vector3 'point'.
    /// </summary>
    public static Vector3 ClosestPointOnLine(Vector3 point, Vector3 lineOrigin, Vector3 lineSecondPosition, out float distance)
    {
        /* http://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line#Vector_formulation
         * where n is a unit vector in the direction of the line, a is a point on the line and t is a scalar.
         * That is, a point x on the line, is found by moving to a point a in space, then moving t units along the direction of the line.
         * || ((a - p) dot n) * n || */

        Vector3 unitDirection = (lineSecondPosition - lineOrigin).normalized;
        Vector3 relativePosToOrigin = lineOrigin - point;
        Vector3 projectedLenOnLine = Vector3.Dot(relativePosToOrigin, unitDirection) * unitDirection;
        distance = Vector3.Distance(projectedLenOnLine, relativePosToOrigin);
        return point + (relativePosToOrigin - projectedLenOnLine);
    }

    /// <summary>
    /// Another method for getting the closest point on a line segmnent from a given point.
    /// </summary>
    public static Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ap = p - a; // vector from A to P   
        Vector3 ab = b - a; // vector from A to B  

        float magnitudeAb = ab.sqrMagnitude; // magnitude of AB vector (it's length squared)     
        float AbApProduct = Vector3.Dot(ap, ab); // dot product of a-to-p and a-to-b     
        float distance = AbApProduct / magnitudeAb; // normalized "distance" from a to your closest point  

        if (distance < 0) // check if p projection is over vectorAB     
        {
            return a;
        }

        return distance > 1 ? b : a + ab * distance;
    }

    /// <summary>
    /// Parse a string representing a Vector4, 3 or 2, into a Vector4.
    /// </summary>
    public static Vector4 ParseVector4(string s)
    {
        s = s.TrimStart('(').TrimEnd(')');
        string[] sArray = s.Split(',');
        Vector4 result = new Vector4(
            float.Parse(sArray[0].Trim()),
            float.Parse(sArray[1].Trim()),
            sArray.Length > 2 ? float.Parse(sArray[2].Trim()) : 0,
            sArray.Length > 3 ? float.Parse(sArray[3].Trim()) : 0);
        return result;
    }

    /// <summary>
    /// Parse a string representing a Vector3, or Vector2 into a Vector3.
    /// </summary>
    public static Vector3 ParseVector3(string s)
    {
        s = s.TrimStart('(').TrimEnd(')');
        string[] sArray = s.Split(',');
        Vector3 result = new Vector3(
            float.Parse(sArray[0].Trim()),
            float.Parse(sArray[1].Trim()),
            sArray.Length > 2 ? float.Parse(sArray[2].Trim()) : 0);
        return result;
    }

    /// <summary>
    /// Parse a string representing a Vector2 into a Vector2.
    /// </summary>
    public static Vector2 ParseVector2(string s)
    {
        s = s.TrimStart('(').TrimEnd(')');
        string[] sArray = s.Split(',');
        Vector3 result = new Vector3(
            float.Parse(sArray[0].Trim()),
            float.Parse(sArray[1].Trim()));
        return result;
    }

    /// <summary>
    /// Parse a string representing a Vector3i, or Vector2i into a Vector3i.
    /// </summary>
    public static int3 ParseInt3(string s)
    {
        Vector3 v = ParseVector3(s);
        return new int3(v);
    }

    /// <summary>
    /// Parse a string representing a Vector2i into a Vector2i.
    /// </summary>
    public static int2 ParseInt2(string s)
    {
        Vector2 v = ParseVector2(s);
        return new int2(v);
    }
}