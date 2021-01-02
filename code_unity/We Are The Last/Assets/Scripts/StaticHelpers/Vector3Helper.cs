using UnityEngine;
using System.Collections.Generic;

public static class Vector3Helper
{

    public static bool IsPointOnLine( Vector3 a, Vector3 b, Vector3 p )
    {
        Vector3 AB = b-a;
        Vector3 AP = p-a;
        float cross = AB.x * AP.y - AP.x * AB.y;

        return Mathf.Abs( cross ) < 0.00001f;
    }
    public static bool IsPointRightOfLine( Vector3 a, Vector3 b, Vector3 p )
    {
        Vector3 AB = b-a;
        Vector3 AP = p-a;
        float cross = AB.x * AP.y - AP.x * AB.y;
        return cross < 0f;
    }
    public static bool IsPointLeftOfLine( Vector3 a, Vector3 b, Vector3 p )
    {
        Vector3 AB = b-a;
        Vector3 AP = p-a;
        float cross = AB.x * AP.y - AP.x * AB.y;
        return cross > 0f;
    }

    public static bool SegmentTouchesOrCrossesLine( Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2 )
    {
        return IsPointOnLine( a1, a2, b1 )
            || IsPointOnLine( a1, a2, b2 )
            || (IsPointRightOfLine( a1, a2, b1 ) != IsPointRightOfLine( a1, a2, b2 ));
    }

    public static bool DoBoundingBoxesIntersect( Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2 )
    {
        Vector3 minA, maxA;
        minA.x = Mathf.Min( a1.x, a2.x );
        minA.y = Mathf.Min( a1.y, a2.y );
        maxA.x = Mathf.Max( a1.x, a2.x );
        maxA.y = Mathf.Max( a1.y, a2.y );

        Vector3 minB, maxB;
        minB.x = Mathf.Min( b1.x, b2.x );
        minB.y = Mathf.Min( b1.y, b2.y );
        maxB.x = Mathf.Max( b1.x, b2.x );
        maxB.y = Mathf.Max( b1.y, b2.y );
        return minA.x <= maxB.x
            && maxA.x >= minB.x
            && minA.y <= maxB.y
            && maxA.y >= minB.y;
    }
    public static bool DoLineSegmentsIntersect( Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2 )
    {
        return DoBoundingBoxesIntersect( a1, a2, b1, b2 )
            && SegmentTouchesOrCrossesLine( a1, a2, b1, b2 )
            && SegmentTouchesOrCrossesLine( b1, b2, a1, a2 );
    }

    public static bool GetSegmentIntersection( Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 intersection )
    {
        intersection = Vector3.zero;
        if ( DoLineSegmentsIntersect( a1, a2, b1, b2 ) )
        {

            return LineLineIntersection( out intersection, a1, a2 - a1, b1, b2 - b1 );
        }

        return false;
    }
    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    //Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
    //same plane, use ClosestPointsOnTwoLines() instead.
    public static bool LineLineIntersection( out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2 )
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if ( Mathf.Abs( planarFactor ) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f )
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    public static bool IsPointRoughlyEqual( Vector3 a, Vector3 b )
    {
        return Vector3.Distance( a, b ) < 0.001f;
    }

    public static bool ArePointsOverlapping( Vector3 a, Vector3 b )
    {
        return (Mathf.Abs( a.x - b.x ) < 0.001f);
    }


    // Points must be passed to this function in a clockwise direction 
    // Edge order doesn't matter
    public static void ClipEdgesToQuad( Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4, List<Vector3> Edges )
    {
        int edgeCount = Edges.Count;

        // Debug2.DrawVector(point1, point2, Color.white, 1.0f);
        // Debug2.DrawVector(point2, point3, Color.white, 1.0f);
        // Debug2.DrawVector(point3, point4, Color.white, 1.0f);
        // Debug2.DrawVector(point4, point1, Color.white, 1.0f);

        for ( int i = 0; i < edgeCount - 1; i += 2 )
        {
            Vector3 edgeA = Edges[i];
            Vector3 edgeB = Edges[i+1];

            // If this returns false, both points are to the left(outside) and should be skipped
            if ( !SingleEdgeClip( point1, point2, ref edgeA, ref edgeB ) )
            {
                //Debug2.DrawVector(point1, point2, Color.white, 1.0f);
                //Debug2.DrawVector(edgeA, edgeB, Color.Lerp(Color.blue,Color.cyan, (float) i / (float) edgeCount), 1.0f);
                continue;
            }

            if ( !SingleEdgeClip( point2, point3, ref edgeA, ref edgeB ) )
            {
                //Debug2.DrawVector(point2, point3, Color.yellow, 1.0f);
                //Debug2.DrawVector(edgeA, edgeB, Color.Lerp(Color.blue,Color.cyan, (float) i / (float) edgeCount), 1.0f);
                continue;
            }


            if ( !SingleEdgeClip( point3, point4, ref edgeA, ref edgeB ) )
            {
                //Debug2.DrawVector(point3, point4, Color.green, 1.0f);
                //Debug2.DrawVector(edgeA, edgeB, Color.Lerp(Color.blue,Color.cyan, (float) i / (float) edgeCount), 1.0f);
                continue;
            }


            if ( !SingleEdgeClip( point4, point1, ref edgeA, ref edgeB ) )
            {

                //Debug2.DrawVector(point4, point1, Color.grey, 1.0f);
                //Debug2.DrawVector(edgeA, edgeB, Color.Lerp(Color.blue,Color.cyan, (float) i / (float) edgeCount), 1.0f);
                continue;
            }


            // If you get this far, all of the points are inside and clipped
            Edges.Add( edgeA );
            Edges.Add( edgeB );
        }

        // Remove the originals
        Edges.RemoveRange( 0, edgeCount );
    }

    // Returns false
    public static bool SingleEdgeClip( Vector3 clipA, Vector3 clipB, ref Vector3 edgeA, ref Vector3 edgeB )
    {
        // If both points are outside of any edge, ignore them, they are both clipped out
        if ( IsPointLeftOfLine( clipA, clipB, edgeA )
            && IsPointLeftOfLine( clipA, clipB, edgeB ) )
        {
            //Debug.DrawLine(edgeA, edgeB, Color.red, 1.0f);

            return false;
        }
        // If both points are on or right of the line, they are clipped in (so add them)
        else if ( !IsPointLeftOfLine( clipA, clipB, edgeA )
            && !IsPointLeftOfLine( clipA, clipB, edgeB ) )
        {
            //Debug.DrawLine(edgeA, edgeB, Color.green, 1.0f);
            // Do nothing! Both points are fine
            return true;
        }
        else
        {
            // Find the intersection
            // Replace the outside vert with it
            Vector3 intersection;

            // If this returns false, the lines are parallel... which should have been caught above
            if ( !LineLineIntersection( out intersection, clipA, clipB - clipA, edgeA, edgeB - edgeA ) )
            {
                //Debug2.DrawVector(clipA, clipB, Color.yellow, 1.0f);
                //Debug2.DrawText(edgeA, edgeA.ToString(), 1.0f, Color.yellow);
                //Debug2.DrawText(edgeB, edgeB.ToString(), 1.0f, Color.Lerp(Color.yellow, Color.red, 0.4f) );
                return false;
            }

            if ( IsPointLeftOfLine( clipA, clipB, edgeA ) )
                edgeA = intersection;
            else
                edgeB = intersection;

            //Debug2.DrawX(intersection, 0.02f, Color.red, 1.0f);

            return true;
        }
    }


    public static Vector2 GetClosestPointOnLineSegment( Vector2 A, Vector2 B, Vector2 P )
    {
        Vector2 AP = P - A;       //Vector from A to P   
        Vector2 AB = B - A;       //Vector from A to B  

        float magnitudeAB = AB.SqrMagnitude();     //Magnitude of AB vector (it's length squared)     
        float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
        float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

        if ( distance < 0 )     //Check if P projection is over vectorAB     
        {
            return A;
        }
        else if ( distance > 1 )
        {
            return B;
        }
        else
        {
            return A + AB * distance;
        }
    }
}