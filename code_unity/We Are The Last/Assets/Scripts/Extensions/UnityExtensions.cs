using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public static class AtomechExtensions
{

    // This one is like FindChild, but will search inactive game objects
    public static Transform Search( this Transform target, string name )
    {
        if ( target.name == name ) return target;

        for ( int i = 0; i < target.childCount; ++i )
        {
            var result = Search(target.GetChild(i), name);

            if ( result != null ) return result;
        }

        return null;
    }

    public static void Reset( this Transform t )
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    public static T GetOrAddComponent<T>( this GameObject go ) where T : Component
    {
        var c = go.GetComponent<T>();
        if ( c == null )
            c = go.AddComponent<T>();
        return c;
    }

    /// <summary>
    /// Returns all monobehaviours (casted to T)
    /// </summary>
    /// <typeparam name="T">interface type</typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T[] GetInterfaces<T>( this GameObject gObj )
    {
        if ( !typeof( T ).IsInterface ) throw new SystemException( "Specified type is not an interface!" );
        var mObjs = gObj.GetComponents<MonoBehaviour>();
        return (from a in mObjs where (a != null && a.GetType().GetInterfaces().Any( k => k == typeof( T ) )) select (T) (object) a).ToArray();
    }

    /// <summary>
    /// Returns the first monobehaviour that is of the interface type (casted to T)
    /// </summary>
    /// <typeparam name="T">Interface type</typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T GetInterface<T>( this GameObject gObj )
    {
        if ( !typeof( T ).IsInterface ) throw new SystemException( "Specified type is not an interface!" );
        return gObj.GetInterfaces<T>().FirstOrDefault();
    }

    /// <summary>
    /// Returns the first instance of the monobehaviour that is of the interface type T (casted to T)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T GetInterfaceInChildren<T>( this GameObject gObj )
    {
        if ( !typeof( T ).IsInterface ) throw new SystemException( "Specified type is not an interface!" );
        return gObj.GetInterfacesInChildren<T>().FirstOrDefault();
    }

    /// <summary>
    /// Returns the first instance of the monobehaviour that is of the interface type T (casted to T)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T GetInterfaceInParent<T>( this GameObject gObj )
    {
        if ( !typeof( T ).IsInterface ) throw new SystemException( "Specified type is not an interface!" );
        var i = gObj.GetInterface<T>();
        if ( i == null && gObj.transform.parent != null )
            i = gObj.transform.parent.gameObject.GetInterfaceInParent<T>();
        return i;
    }


    /// <summary>
    /// Gets all monobehaviours in children that implement the interface of type T (casted to T)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T[] GetInterfacesInChildren<T>( this GameObject gObj )
    {
        if ( !typeof( T ).IsInterface ) throw new SystemException( "Specified type is not an interface!" );

        var mObjs = gObj.GetComponentsInChildren<MonoBehaviour>();

        return (from a in mObjs where a.GetType().GetInterfaces().Any( k => k == typeof( T ) ) select (T) (object) a).ToArray();
    }

    /*
    public static StanceManager AddStanceManager( this TrueSyncBehaviour tsb )
    {
        var stanceMgr = tsb.gameObject.AddComponent<StanceManager>();
        TrueSyncManager.RegisterITrueSyncBehaviour( stanceMgr );
        stanceMgr.owner = tsb.owner;

        return stanceMgr;
    }

    public static AIManager AddAIManager( this TrueSyncBehaviour tsb )
    {
        var aiMgr = tsb.gameObject.AddComponent<AIManager>();
        TrueSyncManager.RegisterITrueSyncBehaviour( aiMgr );
        aiMgr.owner = tsb.owner;

        return aiMgr;
    }

    public static MovableUnit AddMovable( this TrueSyncBehaviour tsb )
    {
        var component = tsb.gameObject.AddComponent<MovableUnit>();
        TrueSyncManager.RegisterITrueSyncBehaviour( component );
        component.owner = tsb.owner;

        return component;
    }

    public static T AddTrueSync<T>( this TrueSyncBehaviour tsb ) where T : TrueSyncBehaviour
    {
        var component = tsb.gameObject.AddComponent<T>();
        TrueSyncManager.RegisterITrueSyncBehaviour( component );
        component.owner = tsb.owner;
        return component;
    }


    // [ABrazie] Maybe put this in StaticPhysicsHelper
    public static bool IsBodyMine( this MonoBehaviour t, TrueSync.Physics2D.Body body )
    {
        var go = TrueSync.Physics2DWorldManager.instance.GetGameObject(body);
        if ( go == null ) return false;
        return go.transform.HasMatchingParentGameObject( t.gameObject );
    }
    public static bool OtherColliderFound( this MonoBehaviour t, TrueSync.Physics2D.Body body )
    {
        var edge = body.ContactList;
        bool contactFound = false;
        while ( edge != null )
        {
            if ( edge.Other.IsSensor )
            {
                if ( t.IsBodyMine( edge.Other ) )
                {
                    body.IgnoreCollisionWith( edge.Other );
                }
                edge = edge.Next;
                continue;
            }

            var overlap = edge.Contact.IsTouching; //TrueSync.Physics2D.Collision.TestOverlap(edge);
            if ( overlap )
            {
                contactFound = !t.IsBodyMine( edge.Other );
                if ( contactFound )
                    break;
                else
                {
                    body.IgnoreCollisionWith( edge.Other );
                }
            }
            edge = edge.Next;
        }

        return contactFound;
    }

    ///
    /// This function is used to snap the grab spot to the nearest vertice inside of a box.
    ///     Where the grab point is chosen based on the TSVector2 forwardUp (min forward, max up)
    ///      This should eventually be replaced with a 'vertex selector' function and a 'nearest point'
    ///         Rather than this madness, but it works, so fuck it.
    ///
    public static TSVector2 GetGrabOffset( this MonoBehaviour t, TSVector2 forwardUp, TrueSync.Physics2D.Body body )
    {
        TSVector2 offset = TSVector2.zero;

        // Grap the contact from the grab box
        var fixture = body.FixtureList[0];
        var poly = fixture.Shape as TrueSync.Physics2D.PolygonShape;
        var verts = poly.Vertices;
        var forward = forwardUp;

        TSVector2 grabPoint = TSVector2.zero;
        bool initialized = false;
        for ( int i = 0; i < verts.Count; i++ )
        {
            var vert = body.GetWorldPoint(verts[i]);
            if ( initialized == false )
            {
                grabPoint = vert;
                initialized = true;
            }
            else if ( (vert.x * forward.x <= grabPoint.x * forward.x) )
            {
                if ( vert.y * forward.y >= grabPoint.y * forward.y )
                {
                    grabPoint = vert;
                }
            }
        }

        //    Debug2.DrawX(grabPoint.ToVector(), 0.5f, Color.green, 3.0f);

        var edge = body.ContactList;
        bool contactFound = false;
        while ( edge != null )
        {
            var overlap = TrueSync.Physics2D.Collision.TestOverlap(edge);
            if ( overlap )
            {
                contactFound = !t.IsBodyMine( edge.Other );
                if ( contactFound )
                {
                    var other = edge.Other;
                    TrueSync.Physics2D.Vertices otherVerts = null;
                    // Quick hack - support other versions in the future
                    if ( other.FixtureList[0].Shape is TrueSync.Physics2D.PolygonShape )
                    {
                        otherVerts = ((TrueSync.Physics2D.PolygonShape) other.FixtureList[0].Shape).Vertices;
                    }
                    else if ( other.FixtureList[0].Shape is TrueSync.Physics2D.EdgeShape )
                    {
                        otherVerts = new TrueSync.Physics2D.Vertices();
                        for ( int j = 0; j < other.FixtureList.Count; j++ )
                        {
                            otherVerts.Add( ((TrueSync.Physics2D.EdgeShape) other.FixtureList[0].Shape).Vertex1 );
                            otherVerts.Add( ((TrueSync.Physics2D.EdgeShape) other.FixtureList[0].Shape).Vertex2 );
                        }
                    }
                    else if ( other.FixtureList[0].Shape is TrueSync.Physics2D.ChainShape )
                    {
                        otherVerts = ((TrueSync.Physics2D.ChainShape) other.FixtureList[0].Shape).Vertices;
                    }
                    else
                    {
                        Debug.LogFormat( "[GrabEdge] {0} is not supported to grab yet!", other.FixtureList[0].GetType() );
                        continue;
                    }

                    TSVector2 corner = TSVector2.zero;
                    FP distance = FP.PositiveInfinity;
                    bool contained = false;
                    for ( int k = 0; k < otherVerts.Count; k++ )
                    {
                        var otherVert = other.GetWorldPoint(otherVerts[k]);
                        var otherContained = fixture.TestPoint( ref otherVert );

                        var otherDistance = TSVector2.DistanceSquared(grabPoint, otherVert);
                        if ( distance == FP.PositiveInfinity )
                        {
                            corner = otherVert;
                            distance = otherDistance;
                            contained = otherContained;
                        }
                        else if ( otherDistance < distance || (otherContained && contained == false) )
                        {
                            corner = otherVert;
                            distance = otherDistance;
                            contained = otherContained;
                        }

                        //Debug2.DrawX(otherVert.ToVector(), 0.25f, Color.red, 3f);
                    }

                    //Debug2.DrawX(corner.ToVector(), 0.25f, Color.cyan, 3f);

                    if ( distance != FP.PositiveInfinity )
                    {
                        offset = corner - grabPoint;
                    }
                }
            }
            edge = edge.Next;
        }

        return offset;
    }
    */
}


public class Debug2Mono : MonoBehaviour // Vexe.Runtime.Types.BaseBehaviour
{
    public class Debug2String
    {
        public Vector3 pos;
        public string text;
        public Color? color;
        public float eraseTime;
    }

    public List<Debug2String> Strings = new List<Debug2String>();

    public void OnDrawGizmos()
    {
        foreach ( var stringpair in Strings )
        {
            GUIStyle style = new GUIStyle();
            Color color = stringpair.color ?? Color.green;
            style.normal.textColor = color;

#if UNITY_EDITOR            
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label( stringpair.pos, stringpair.text, style );
#endif
        }
    }
}
public static class Debug2
{
    // Debug Draw
    public static void DrawX( Vector3 center, float size, Color color, float duration )
    {
        Debug.DrawLine( center + (new Vector3( -1f, -1f ) * size), center + (new Vector3( 1f, 1f ) * size), color, duration );
        Debug.DrawLine( center + (new Vector3( 1f, -1f ) * size), center + (new Vector3( -1f, 1f ) * size), color, duration );
    }

    public static void DrawCircle( Vector3 center, float radius, Color color, float duration, int detail = 16 )
    {
        Vector3 prev = new Vector3(Mathf.Cos(0)*radius, Mathf.Sin(0)*radius, 0f) + center;

        for ( int i = 0; i <= detail; i++ )
        {
            float t = (i*Mathf.PI*2f)/detail;
            Vector3 c = new Vector3(Mathf.Cos(t)*radius, Mathf.Sin(t)*radius, 0f) + center;
            Debug.DrawLine( prev, c, color, duration );
            prev = c;
        }
    }

    public static void DrawDiamond ( Vector3 center, Vector2 size, Color color, float duration )
    {
        var DiamondWidth = size.x * 0.5f;
        var DiamondHeight = size.y * 0.5f;

        Debug.DrawLine( center + Vector3.right * DiamondWidth, center + Vector3.up * DiamondHeight );
        Debug.DrawLine( center + Vector3.left * DiamondWidth, center + Vector3.up * DiamondHeight );
        Debug.DrawLine( center + Vector3.right * DiamondWidth, center + Vector3.down * DiamondHeight );
        Debug.DrawLine( center + Vector3.left * DiamondWidth, center + Vector3.down * DiamondHeight );

    }

    public static void DrawRectangle( Vector3 center, Vector2 size, Color color, float duration )
    {
        Debug.DrawLine( center + (new Vector3( -1f, -1f ) * size.x), center + (new Vector3( 1f, -1f ) * size.y), color, duration );
        Debug.DrawLine( center + (new Vector3( 1f, -1f ) * size.x), center + (new Vector3( 1f, 1f ) * size.y), color, duration );
        Debug.DrawLine( center + (new Vector3( 1f, 1f ) * size.x), center + (new Vector3( -1f, 1f ) * size.y), color, duration );
        Debug.DrawLine( center + (new Vector3( -1f, 1f ) * size.x), center + (new Vector3( -1f, -1f ) * size.y), color, duration );
    }

    public static void DrawVector( Vector3 start, Vector3 end, Color color, float duration, float headPercentage = 0.25f )
    {
        Debug.DrawLine( start, end, color, duration );

        Vector3 perp = Vector3.Cross(start-end, Vector3.forward);
        float headLength = (start-end).magnitude * headPercentage;
        Vector3 back = end + (start-end).normalized * headLength;

        Debug.DrawLine( end, back + (perp * headPercentage) * 0.35f, color, duration );
        Debug.DrawLine( end, back - (perp * headPercentage) * 0.35f, color, duration );
    }

    private static Debug2Mono m_instance;
    public static Debug2Mono Instance
    {
        get
        {
            if ( m_instance == null )
            {
                m_instance = GameObject.FindObjectOfType<Debug2Mono>();
                if ( m_instance == null )
                {
                    var go = new GameObject("DeleteMeLater");
                    m_instance = go.AddComponent<Debug2Mono>();
                }
            }
            return m_instance;
        }
    }

    public static void DrawText( Vector3 pos, string text, float duration, Color? color = null )
    {
        Instance.Strings.Add( new Debug2Mono.Debug2String() { text = text, color = color, pos = pos, eraseTime = Time.time + duration, } );

        List<Debug2Mono.Debug2String> toBeRemoved = new List<Debug2Mono.Debug2String>();

        foreach ( var item in Instance.Strings )
        {
            if ( item.eraseTime <= Time.time )
                toBeRemoved.Add( item );
        }

        foreach ( var rem in toBeRemoved )
            Instance.Strings.Remove( rem );
    }
}

public static class GizmoHelper
{
    public static void DrawCircleGizmo( Vector3 center, float radius, Color color )
    {
        InControl.Utility.DrawCircleGizmo( center, radius, color );
    }
    public static void DrawOvalGizmo( Vector3 center, Vector2 size, Color color )
    {
        InControl.Utility.DrawOvalGizmo( center, size, color );
    }
    public static void DrawRectGizmo( Rect rect, Color color )
    {
        InControl.Utility.DrawRectGizmo( rect, color );
    }

    public static void DrawRectGizmo( Vector3 center, Vector2 rect, Color color )
    {
        InControl.Utility.DrawRectGizmo( new Rect( center, rect ), color );
    }


    public static void DrawXGizmo( Vector3 center, float size, Color color )
    {
        Gizmos.color = color;
        Gizmos.DrawLine( center + (new Vector3( -1f, -1f ) * size), center + (new Vector3( 1f, 1f ) * size) );
        Gizmos.DrawLine( center + (new Vector3( 1f, -1f ) * size), center + (new Vector3( -1f, 1f ) * size) );
    }
}
