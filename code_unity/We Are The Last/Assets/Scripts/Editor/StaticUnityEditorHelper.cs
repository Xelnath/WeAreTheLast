using UnityEngine;
using UnityEditor;
using Sini.Unity;
using Sirenix.Utilities.Editor;
using System;
using System.IO;
using UnityEngine.SceneManagement;


public static partial class StaticUnityEditorHelper
{
    public static Vector3 SceneViewWorldToScreenPoint(SceneView sv, Vector2 worldPos)
    {
        var style = (GUIStyle)"GV Gizmo DropDown";
        Vector2 ribbon = style.CalcSize(sv.titleContent);

        Vector2 sv_correctSize = sv.position.size;
        sv_correctSize.y -= ribbon.y; //exclude this nasty ribbon
        
        //gives coordinate inside SceneView context.
        // WorldToViewportPoint() returns 0-to-1 value, where 0 means 0% and 1.0 means 100% of the dimension
        Vector3 pointInView = sv.camera.WorldToViewportPoint(worldPos);
        Vector3 pointInSceneView = pointInView * sv_correctSize;
        var p1 = pointInSceneView;
        p1.y = sv.position.height - p1.y;

        return p1;
    }

    public static void DrawVector2_Vertical( string label, Vector2 vec )
	{
		GUILayout.BeginVertical();
		if ( string.IsNullOrEmpty(label) == false )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Box( label, GUILayout.ExpandWidth( true ) );
			GUILayout.EndHorizontal();
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label( "X:" );
		GUILayout.TextField( vec.x.ToString() );
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label( "Y:" );
		GUILayout.TextField( vec.y.ToString() );
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	public static void DrawVector3_Vertical( string label, Vector3 vec )
	{
		GUILayout.BeginVertical();
		if ( string.IsNullOrEmpty( label ) == false )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Box( label, GUILayout.ExpandWidth( true ) );
			GUILayout.EndHorizontal();
		}
		GUILayout.BeginHorizontal();
		GUILayout.Label( "X:" );
		GUILayout.TextField( vec.x.ToString() );
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label( "Y:" );
		GUILayout.TextField( vec.y.ToString() );
		GUILayout.BeginHorizontal();
		GUILayout.Label( "Z:" );
		GUILayout.TextField( vec.z.ToString() );
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

	}
	public static void DrawVector2( string label, Vector2 vec )
	{


		if ( string.IsNullOrEmpty( label ) )
			SirenixEditorFields.Vector2Field( vec );
		else
			SirenixEditorFields.Vector2Field( label, vec );

	}
	// public static void DrawVector3( string label, FPVector3 vec )
	// {
	// 	if ( string.IsNullOrEmpty( label ) )
	// 		SirenixEditorFields.Vector2Field( vec.ToUnityVector2() );
	// 	else
	// 		SirenixEditorFields.Vector2Field( label, vec.ToUnityVector2() );
	// }
	public static void DrawVector2( Vector2 vec )
	{
		SirenixEditorFields.Vector2Field( vec );
	}
	public static void DrawVector3( Vector3 vec )
	{
		SirenixEditorFields.Vector3Field( vec );
	}
	public static void DrawWedge( int AngleMin, int AngleMax, Material mat, float width, float height, Color color, int steps = 16 )
	{
		GUILayout.BeginHorizontal();
		Rect wholeRect = GUILayoutUtility.GetRect(width,height);
		Rect outer = new Rect(0,0,width,height).InnerAlignWithCenterLeft(wholeRect);

		DrawWedge( outer, AngleMin, AngleMax, mat, color, steps );
		GUILayout.EndHorizontal();
	}
	// public static void DrawVectorArrow( FPVector2 vec, FP Rotation, Material mat, float width, float height, Color color )
	// {
	// 	GUILayout.BeginHorizontal();
	// 	Rect wholeRect = GUILayoutUtility.GetRect(width,height);
	// 	Rect outer = new Rect(0,0,width,height).InnerAlignWithCenterLeft(wholeRect);
	//
	// 	DrawVectorArrow( outer, vec, Rotation, mat, color );
	// 	GUILayout.EndHorizontal();
	// }

	public static void DrawWedge( Rect outer, int AngleMin, int AngleMax, Material mat, Color color, int steps = 16 )
	{
		if ( mat == null )
			return;

		float pad = 2;
		float radius = outer.width*0.5f-pad;

		Rect inner = outer.PadSides(pad);
		Rect drawRect = inner.PadSides(pad);
		Vector3 center = drawRect.size * 0.5f;
		radius = drawRect.width * 0.5f;
		EditorGUI.DrawRect( outer, Color.grey );
		EditorGUI.DrawRect( inner, Color.black );
		
		if ( Event.current.type == EventType.Repaint )
		{
			GUI.BeginClip( drawRect );
			GL.PushMatrix();
			GL.Clear( true, false, Color.black );
			mat.SetPass( 0 );

			int step = 360 / steps;

			GL.Begin( GL.TRIANGLES );
			GL.Color( color );
			for ( int a = AngleMin; a < AngleMax; a += step )
			{
				int stepNext = Mathf.Min(AngleMax, a+step);
				float angA = a * Mathf.Deg2Rad;
				float angB = stepNext * Mathf.Deg2Rad;
				Vector3 pointA = new Vector3( Mathf.Cos(angA), -Mathf.Sin( angA ) ) * radius;
				Vector3 pointB = new Vector3( Mathf.Cos(angB), -Mathf.Sin( angB ) ) * radius;
				GL.Vertex( center );
				GL.Vertex( center + pointA );
				GL.Vertex( center + pointB );
			}
			GL.End();

			GL.Begin( GL.LINES );
			GL.Color( Color.white );
			for ( int a = 0; a < 360; a += step )
			{
				float angA = a * Mathf.Deg2Rad;
				float angB = (a+step) * Mathf.Deg2Rad;
				Vector3 pointA = new Vector3( Mathf.Cos(angA), Mathf.Sin( angA )  ) * radius;
				Vector3 pointB = new Vector3( Mathf.Cos(angB), Mathf.Sin( angB )  ) * radius;
				GL.Vertex( center + pointA );
				GL.Vertex( center + pointB );
			}
			GL.End();

			GL.PopMatrix();
			GUI.EndClip();
		}
	}
	//
	// public static void DrawShape( Rect outer, FP Rotation, Shape2DConfig shape, Material mat )
	// {
	// 	if ( mat == null )
	// 		return;
	//
	// 	if ( shape.ShapeType == Shape2DType.Circle )
	// 	{
	// 		DrawWedge( outer, 0, 360, mat, PolymerColor.paper_green_500.ToColor().WithAlpha( 0.7f ) );
	// 	}
	// 	else if ( shape.ShapeType == Shape2DType.Polygon )
	// 	{
	// 		var poly = UnityDB.FindAsset <PolygonColliderAsset>(shape.PolygonCollider.Id);
	// 		DrawPolygonCollider( outer, poly.Settings, Rotation, mat, PolymerColor.paper_orange_600.ToColor().WithAlpha( 0.7f ) );
	// 	}
	// 	else if ( shape.ShapeType == Shape2DType.Box )
	// 	{
	// 		var size = shape.BoxExtents * 2;
	// 		DrawRect( outer, size, Rotation, mat, PolymerColor.paper_red_600.ToColor().WithAlpha(0.7f) );
	// 	}
	//
	// }
	//
	// public static void DrawRect( Rect outer, FPVector2 size, FP Rotation, Material mat, Color color )
	// {
	// 	float pad = 2;
	// 	float radius = outer.width*0.5f-pad;
	//
	// 	Rect inner = outer.PadSides(pad);
	// 	Rect drawRect = inner.PadSides(pad);
	// 	Vector3 center = drawRect.size * 0.5f;
	// 	radius = drawRect.width * 0.5f;
	// 	EditorGUI.DrawRect( outer, Color.grey );
	// 	EditorGUI.DrawRect( inner, Color.black );
	//
	// 	var rect = new Rect(0,0, size.X.AsFloat, size.Y.AsFloat);
	// 	rect.center = rect.center - (rect.size * 0.5f);
	//
	// 	FP ratio = FPMath.Max(size.X, size.Y);
	//
	// 	FPVector2 aa= -size / ratio;
	// 	FPVector2 cc = size / ratio;
	// 	FPVector2 bb = new FPVector2(aa.X,cc.Y);
	// 	FPVector2 dd = new FPVector2(cc.X,aa.Y);
	//
	// 	var matrix2x2 = new FPMatrix2x2
	// 	{
	// 		M00 = FPMath.Cos( Rotation ),
	// 		M01 = FPMath.Sin( Rotation ),
	// 		M10 = -FPMath.Sin( Rotation ),
	// 		M11 = FPMath.Cos( Rotation )
	// 	};
	//
	// 	Vector3 a = matrix2x2.MultiplyVector(aa).ToUnityVector3() * radius;
	// 	Vector3 b = matrix2x2.MultiplyVector(bb).ToUnityVector3() * radius;
	// 	Vector3 c = matrix2x2.MultiplyVector(cc).ToUnityVector3() * radius;
	// 	Vector3 d = matrix2x2.MultiplyVector(dd).ToUnityVector3() * radius;
	//
	// 	if ( Event.current.type == EventType.Repaint )
	// 	{
	// 		GUI.BeginClip( drawRect );
	// 		GL.PushMatrix();
	// 		GL.Clear( true, false, Color.black );
	// 		mat.SetPass( 0 );
	//
	// 		GL.Begin( GL.TRIANGLES );
	// 		GL.Color( color );
	// 		GL.Vertex( center );
	// 		GL.Vertex( center + a );
	// 		GL.Vertex( center + b );
	// 		GL.Vertex( center );
	// 		GL.Vertex( center + b );
	// 		GL.Vertex( center + c );
	// 		GL.Vertex( center );
	// 		GL.Vertex( center + c );
	// 		GL.Vertex( center + d );
	// 		GL.Vertex( center );
	// 		GL.Vertex( center + d );
	// 		GL.Vertex( center + a );
	// 		GL.End();
	//
	// 		GL.Begin( GL.LINES );
	// 		GL.Color( Color.white );
	// 		GL.Vertex( center + a );
	// 		GL.Vertex( center + b );
	// 		GL.Vertex( center + b );
	// 		GL.Vertex( center + c );
	// 		GL.Vertex( center + c );
	// 		GL.Vertex( center + d );
	// 		GL.Vertex( center + d );
	// 		GL.Vertex( center + a );
	// 		GL.End();
	//
	// 		GL.PopMatrix();
	// 		GUI.EndClip();
	// 	}
	// }
 //
	// public unsafe static void DrawStateLayerGroups( Rect outer, StateLayer* stateLayer, List<string> GroupNames, ref int selectedStateGroup, ref int currentStateGroup, Material mat )
	// {
	// 		if ( mat == null )
	// 			return;
 //
	// 	if ( GroupNames.Count == 0 )
	// 	{
	// 		GUI.Label( outer, "No groups defined..." );
	// 		return;
	// 	}
 //
	// 	int cols = (int) Mathf.Sqrt(GroupNames.Count);
	// 	if ( cols < 3 ) cols = 3;
	// 	int rows = GroupNames.Count / cols;
	// 	if ( rows == 0 )
	// 		rows = 1;
 //
	// 	float colWidth = outer.width / cols;
 //
	// 	Rect cellSize = new Rect(0f,0f,colWidth, 40f);
 //
	// 	float pad = 4;
	// 	Rect inner = outer.PadSides(pad);
	// 	Rect drawRect = cellSize.PadSides(pad);
 //
	// 	if ( stateLayer != null )
	// 	{
	// 		if ( Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown )
	// 		{
	// 			GUI.BeginClip( drawRect );
	// 			GL.PushMatrix();
	// 			GL.Clear( true, false, Color.black );
	// 			mat.SetPass( 0 );
 //
	// 			for ( int i = 0; i < GroupNames.Count; i++ )
	// 			{
	// 				bool isCurrent = ( i == currentStateGroup );
	// 				Color stateColor = isCurrent ? PolymerColor.paper_amber_400.ToColor() : PolymerColor.paper_green_400.ToColor();
 //
	// 				int row = i / cols;
	// 				int col = i % cols;
 //
	// 				Rect cell = drawRect.Translate(cellSize.width * col, cellSize.height * row);
 //
	// 				GL.Begin( GL.TRIANGLES );
	// 				GL.Color( stateColor );
 //
	// 				Vector3 a = cell.min;
	// 				Vector3 c = cell.max;
	// 				Vector3 b = new Vector3(c.x, a.y);
	// 				Vector3 d = new Vector3(a.x, c.y);
 //
	// 				GL.Vertex( a );
	// 				GL.Vertex( b );
	// 				GL.Vertex( d );
	// 				GL.Vertex( b );
	// 				GL.Vertex( c );
	// 				GL.Vertex( d );
 //
	// 				GL.End();
 //
	// 				GL.Begin( GL.LINES );
	// 				GL.Color( Color.white );
	// 				GL.Vertex( a );
	// 				GL.Vertex( b );
	// 				GL.Vertex( b );
	// 				GL.Vertex( c );
	// 				GL.Vertex( c );
	// 				GL.Vertex( d );
	// 				GL.Vertex( d );
	// 				GL.Vertex( a );
	// 				GL.End();
 //
	// 			}
 //
	// 			GL.PopMatrix();
	// 			GUI.EndClip();
 //
 //
	// 			for ( int i = 0; i < GroupNames.Count; i++ )
	// 			{
	// 				int row = i / cols;
	// 				int col = i % cols;
 //
	// 				Rect cell = drawRect.Translate(cellSize.width * col, cellSize.height * row);
 //
	// 				GUIHelper.PushContentColor( Color.black );
	// 				GUI.Label( cell, GroupNames[i], SirenixGUIStyles.LabelCentered );
	// 				GUIHelper.PopContentColor();
 //
	// 				if (Event.current.type == EventType.MouseDown && cell.Contains(Event.current.mousePosition) )
	// 					selectedStateGroup = (selectedStateGroup != i) ? i : -1;
	// 			}
	// 		}
	// 	}
	// }
 //
	// public unsafe static void DrawStateLayerGroupStates( Frame f, Rect outer, StateLayer* stateLayer, List<string> GroupNames, List<string> States, ref int selectedStateGroup, ref int currentStateGroup, Material mat )
	// {
	// 	if ( mat == null )
	// 		return;
 //
	// 	int cellCount = GroupNames.Count + States.Count;
 //
	// 	int cols = (int) Mathf.Sqrt(cellCount);
	// 	if ( cols < 3 ) cols = 3;
	// 	int rows = cellCount / cols;
 //
	// 	if ( rows < GroupNames.Count )
	// 		rows = GroupNames.Count;
 //
 //
	// 	float colWidth = outer.width / cols;
 //
	// 	Rect cellSize = new Rect(0f,0f,colWidth, 40f);
 //
	// 	float pad = 4;
	// 	Rect inner = outer.PadSides(pad);
	// 	Rect drawRect = cellSize.PadSides(pad);
 //
	// 	if ( stateLayer != null )
	// 	{
	// 		if ( Event.current.type == EventType.Repaint  || Event.current.type == EventType.MouseDown )
	// 		{
	// 			GUI.BeginClip( drawRect );
	// 			GL.PushMatrix();
	// 			GL.Clear( true, false, Color.black );
	// 			mat.SetPass( 0 );
 //
	// 			for ( int i = 0; i < cellCount; i++ )
	// 			{
	// 				int row = i % rows;
	// 				int col = i / rows;
 //
	// 				Rect cell = drawRect.Translate(cellSize.width * col, cellSize.height * row);
 //
	// 				bool isCurrent;
	// 				Color stateColor;
	// 				if ( i < GroupNames.Count )
	// 				{
	// 					isCurrent = (i == currentStateGroup); ;
	// 					stateColor = isCurrent ? PolymerColor.paper_amber_400.ToColor() :
	// 						((i == selectedStateGroup) ? PolymerColor.paper_blue_grey_500.ToColor() : PolymerColor.paper_green_400.ToColor());
	// 				}
	// 				else
	// 				{
	// 					var history = f.ResolveList(stateLayer->History);
	// 					var currentState = f.FindAsset<MachineState>(history[0].StateEntered.Id);
 //
	// 					isCurrent = (currentState.Name == States[i-GroupNames.Count]);
	// 					stateColor = isCurrent ? PolymerColor.paper_amber_400.ToColor() : PolymerColor.paper_grey_400.ToColor();
	// 				}
 //
	// 				GL.Begin( GL.TRIANGLES );
	// 				GL.Color( stateColor );
 //
	// 				Vector3 a = cell.min;
	// 				Vector3 c = cell.max;
	// 				Vector3 b = new Vector3(c.x, a.y);
	// 				Vector3 d = new Vector3(a.x, c.y);
 //
	// 				GL.Vertex( a );
	// 				GL.Vertex( b );
	// 				GL.Vertex( d );
	// 				GL.Vertex( b );
	// 				GL.Vertex( c );
	// 				GL.Vertex( d );
 //
	// 				GL.End();
 //
	// 				GL.Begin( GL.LINES );
	// 				GL.Color( Color.white );
	// 				GL.Vertex( a );
	// 				GL.Vertex( b );
	// 				GL.Vertex( b );
	// 				GL.Vertex( c );
	// 				GL.Vertex( c );
	// 				GL.Vertex( d );
	// 				GL.Vertex( d );
	// 				GL.Vertex( a );
	// 				GL.End();
 //
	// 			}
 //
	// 			GL.PopMatrix();
	// 			GUI.EndClip();
 //
 //
	// 			for ( int i = 0; i < cellCount; i++ )
	// 			{
	// 				int row = i % rows;
	// 				int col = i / rows;
 //
	// 				Rect cell = drawRect.Translate(cellSize.width * col, cellSize.height * row);
 //
	// 				if ( i < GroupNames.Count )
	// 				{
	// 					GUIHelper.PushContentColor( Color.black );
	// 					GUI.Label( cell, GroupNames[i], SirenixGUIStyles.LabelCentered );
	// 					GUIHelper.PopContentColor();
 //
 //
	// 					if ( Event.current.type == EventType.MouseDown && cell.Contains( Event.current.mousePosition ) )
	// 						selectedStateGroup = (selectedStateGroup != i) ? i : -1;
	// 				}
	// 				else
	// 				{
	// 					GUIHelper.PushContentColor( Color.black );
	// 					GUI.Label( cell, States[i-GroupNames.Count], SirenixGUIStyles.LabelCentered );
	// 					GUIHelper.PopContentColor();
 //
	// 				}
 //
	// 			}
	// 		}
	// 	}
	// }
 //
	// public static void DrawPolygonCollider( Rect outer, AssetRefPolygonCollider polygon, FP Rotation, Material mat, Color color )
	// {
	// 	if ( mat == null )
	// 		return;
 //
	// 	PolygonCollider poly = UnityDB.FindAsset<PolygonColliderAsset>(polygon.Id).Settings;
 //
	// 	float pad = 2;
	// 	float radius = outer.width*0.5f-pad;
 //
	// 	Rect inner = outer.PadSides(pad);
	// 	Rect drawRect = inner.PadSides(pad);
	// 	Vector3 center = drawRect.size * 0.5f;
	// 	radius = drawRect.width * 0.5f;
	// 	EditorGUI.DrawRect( outer, Color.grey );
	// 	EditorGUI.DrawRect( inner, Color.black );
 //
	// 	if ( poly != null )
	// 	{
	// 		FPBounds2 fpb = new FPBounds2();
	// 		for ( int i = 0; i < poly.Vertices.Length; ++i )
	// 		{
	// 			fpb.Encapsulate( poly.Vertices[i] );
	// 		}
 //
	// 		FP ratio = FPMath.Max(fpb.Extents.Y, fpb.Extents.X);
 //
	// 		var matrix2x2 = new FPMatrix2x2
	// 		{
	// 			M00 = FPMath.Cos( Rotation ),
	// 			M01 = FPMath.Sin( Rotation ),
	// 			M10 = -FPMath.Sin( Rotation ),
	// 			M11 = FPMath.Cos( Rotation )
	// 		};
 //
	// 		if ( Event.current.type == EventType.Repaint )
	// 		{
	// 			GUI.BeginClip( drawRect );
	// 			GL.PushMatrix();
	// 			GL.Clear( true, false, Color.black );
	// 			mat.SetPass( 0 );
 //
	// 			GL.Begin( GL.TRIANGLES );
	// 			GL.Color( color );
	// 			for ( int i = 0; i < poly.Vertices.Length; ++i )
	// 			{
	// 				int next = i + 1 < poly.Vertices.Length ? i + 1 : 0;
	// 				Vector3 pointA = (matrix2x2.MultiplyVector(poly.Vertices[i] / ratio)).ToUnityVector3();
	// 				Vector3 pointB = (matrix2x2.MultiplyVector(poly.Vertices[next] / ratio)).ToUnityVector3();
	// 				GL.Vertex( center );
	// 				GL.Vertex( center + pointA );
	// 				GL.Vertex( center + pointB );
	// 			}
	// 			GL.End();
 //
	// 			GL.Begin( GL.LINES );
	// 			GL.Color( Color.white );
	// 			for ( int i = 0; i < poly.Vertices.Length; ++i )
	// 			{
	// 				int next = i + 1 < poly.Vertices.Length ? i + 1 : 0;
	// 				Vector3 pointA = (matrix2x2.MultiplyVector(poly.Vertices[i] / ratio)).ToUnityVector3();
	// 				Vector3 pointB = (matrix2x2.MultiplyVector(poly.Vertices[next] / ratio)).ToUnityVector3();
	// 				GL.Vertex( center + pointA );
	// 				GL.Vertex( center + pointB );
	// 			}
	// 			GL.End();
 //
	// 			GL.PopMatrix();
	// 			GUI.EndClip();
	// 		}
	// 	}
	// }
 //
 //
	// public static void DrawVectorArrow( Rect outer, FPVector2 vector, FP Rotation, Material mat, Color color )
	// {
	// 	if ( mat == null )
	// 		return;
 //
	// 	float pad = 2;
 //
	// 	Rect inner = outer.PadSides(pad);
	// 	Rect drawRect = inner.PadSides(pad);
	// 	Vector3 center = drawRect.size * 0.5f;
 //        float radius = drawRect.width * 0.5f;
	// 	EditorGUI.DrawRect( outer, Color.grey );
	// 	EditorGUI.DrawRect( inner, Color.black );
 //
 //        QuantumRunner.Init();
 //
	// 	if ( vector.Magnitude == 0 )
	// 		return;
 //
	// 	var matrix2x2 = new FPMatrix2x2
	// 	{
	// 		M00 = FPMath.Cos( Rotation ),
	// 		M01 = FPMath.Sin( Rotation ),
	// 		M10 = -FPMath.Sin( Rotation ),
	// 		M11 = FPMath.Cos( Rotation )
	// 	};
 //
	// 	FP ratio = vector.Magnitude;
	// 	FPVector2 vec = (ratio > 1) ? (matrix2x2.MultiplyVector( vector.Normalized)) : matrix2x2.MultiplyVector(vector);
	// 	Vector3 magDir = vec.ToUnityVector3();
	// 	// Because GL space is reversed
	// 	magDir.y *= -1f;
	// 	Vector3 normal = magDir.normalized;
	// 	Vector3 perp = new Vector3(-normal.y, normal.x);
 //
	// 	if ( Event.current.type == EventType.Repaint )
	// 	{
	// 		GUI.BeginClip( drawRect );
	// 		GL.PushMatrix();
	// 		GL.Clear( true, false, Color.black );
	// 		mat.SetPass( 0 );
 //
	// 		Vector3 edgePoint =  magDir*radius;
	// 		Vector3 edgePoint2 = magDir*radius*0.75f;
	// 		Vector3 lHeadPoint = magDir*radius*0.55f - perp*radius*0.35f;
	// 		Vector3 rHeadPoint = magDir*radius*0.55f + perp*radius*0.35f;
	// 		Vector3 lRootPoint = magDir*radius*-0.35f - perp*radius*0.15f;
	// 		Vector3 rRootPoint = magDir*radius*-0.35f + perp*radius*0.15f;
	// 		Vector3 backSpace =  magDir*radius*-0.15f;
 //
	// 		GL.Begin( GL.TRIANGLES );
	// 		GL.Color( color );
	// 		GL.Vertex( center + edgePoint );
	// 		GL.Vertex( center + edgePoint2 );
	// 		GL.Vertex( center + lHeadPoint );
 //
	// 		GL.Vertex( center + edgePoint );
	// 		GL.Vertex( center + edgePoint2 );
	// 		GL.Vertex( center + rHeadPoint );
 //
	// 		GL.Vertex( center + edgePoint );
	// 		GL.Vertex( center + lRootPoint );
	// 		GL.Vertex( center + backSpace );
	// 		GL.Vertex( center + edgePoint );
	// 		GL.Vertex( center + rRootPoint );
	// 		GL.Vertex( center + backSpace );
	// 		GL.End();
 //
	// 		GL.PopMatrix();
	// 		GUI.EndClip();
	// 	}
	// }

	public static bool BeginClickableBox( string label, bool labelIsCentered, bool toggled )
	{
		var clickRect = SirenixEditorGUI.BeginBox( label, labelIsCentered );

		Event current = Event.current;
		Rect clickArea = clickRect.WithHeight(20).InnerAlignWithUpperCenter(clickRect);
		if ( current.type != EventType.Layout )
		{
			if ( clickArea.Contains( current.mousePosition ) )
			{
				if ( current.type == EventType.Repaint )
				{
					var grey = Color.gray;
					grey.a = 0.5f;
					EditorGUI.DrawRect( clickArea, grey );
				}
				if ( current.type == EventType.MouseDown )
					toggled = !toggled;
			}
		}

		return toggled;
	}

	public static void EndClickableBox()
	{
		SirenixEditorGUI.EndBox();
	}
	//
	// public static void DrawQuantumAssetDrawer( Rect position, string label, AssetGuid guid, Type type = null )
	// {
	// 	var all = UnityEngine.Resources.LoadAll<AssetBase>("DB");
	// 	EditorGUI.ObjectField( position, label, all.FirstOrDefault( ObjectFilter( guid, type ) ), type, false );
	//
	// }
	//
	// static Func<AssetBase, Boolean> ObjectFilter( AssetGuid guid, Type type )
	// {
	// 	return obj => obj && type.IsAssignableFrom( obj.GetType() ) && obj.AssetObject != null && obj.AssetObject.Guid == guid;
	// }
	//


	public static TWindow GetWindowOfType<TWindow>( string windowName ) where TWindow : EditorWindow
	{
		// Check if window already exists. Because, if it does, then we don't have to trigger setup for it
		// I humbly dare to use FindObjects here, as operation is not hapenning every frame
		var existedBefore = Resources.FindObjectsOfTypeAll<TWindow>().Length > 0;
		var window = EditorWindow.GetWindow<TWindow>(windowName,false);
		if ( !existedBefore )
		{

			return null;
		}
		window.Focus();
		return window;
	}

	public static EditorWindow GetWindowOfType( Type t, string windowName )
	{
		// Check if window already exists. Because, if it does, then we don't have to trigger setup for it
		// I humbly dare to use FindObjects here, as operation is not hapenning every frame
		var existedBefore = Resources.FindObjectsOfTypeAll(t).Length > 0;
		var window = EditorWindow.GetWindow(t, false, windowName, false);
		if ( !existedBefore )
		{

			return null;
		}
		window.Focus();
		return window;
	}

	public static System.Type[] GetAllEditorWindowTypes()
	{
		var result = new System.Collections.Generic.List<System.Type>();
		System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
		System.Type editorWindow = typeof(EditorWindow);
		foreach ( var A in AS )
		{
			System.Type[] types = A.GetTypes();
			foreach ( var T in types )
			{
				if ( T.IsSubclassOf( editorWindow ) )
					result.Add( T );
			}
		}
		return result.ToArray();
	}

	public static string[] findAllScenePaths()
	{
		var guids = AssetDatabase.FindAssets("t:Scene");
		var paths = Array.ConvertAll<string, string>(guids, AssetDatabase.GUIDToAssetPath);
		paths = Array.FindAll( paths, File.Exists ); // Unity erroneously considers folders named something.unity as scenes, remove them
		return paths;
	}


    public static string userMapDataRoot = "Assets/resources/DB/scenesUserData/";
    public static void GetSceneNameAndPath(out string scenePath, out string sceneName)
    {
        scenePath = SceneManager.GetActiveScene().path;
        var startPos = scenePath.LastIndexOf("/Assets/scenes/", StringComparison.Ordinal) + "/Assets/scenes/".Length;
        var length = scenePath.IndexOf(".unity", StringComparison.Ordinal) - startPos;
        sceneName = scenePath.Substring(startPos, length);

    }

    public static string GetMapNodesFolder()
    {
        GetSceneNameAndPath(out string scenePath, out string sceneName);
        var userMapDataPath = userMapDataRoot + sceneName + ".asset";
        var userMapDataFolderPath = userMapDataRoot + sceneName;
        var graphPath = userMapDataRoot + sceneName + "/MapGraph";

        if (AssetDatabase.IsValidFolder(scenePath+"/"+sceneName) == false)
            AssetDatabase.CreateFolder(scenePath, sceneName);
        if (AssetDatabase.IsValidFolder(graphPath) == false)
            AssetDatabase.CreateFolder(userMapDataFolderPath, "MapGraph");
        if (AssetDatabase.IsValidFolder(graphPath + "/Nodes") == false)
            AssetDatabase.CreateFolder(graphPath, "Nodes");
        if(AssetDatabase.IsValidFolder(graphPath + "/Connections") == false)
            AssetDatabase.CreateFolder(graphPath, "Connections");

        return graphPath;
    }


    public static string GetNetworksFolder()
    {
        GetSceneNameAndPath(out string scenePath, out string sceneName);
        var userMapDataPath = userMapDataRoot + sceneName + ".asset";
        var userMapDataFolderPath = userMapDataRoot + sceneName;
        var networksPath = userMapDataRoot + sceneName + "/Networks";

        if(AssetDatabase.IsValidFolder(networksPath) == false)
            AssetDatabase.CreateFolder(userMapDataFolderPath, "Networks");
        /*
		if ( AssetDatabase.IsValidFolder( networksPath + "/Nodes" ) == false )
			AssetDatabase.CreateFolder( networksPath, "Nodes" );
		if ( AssetDatabase.IsValidFolder( networksPath + "/Connections" ) == false )
			AssetDatabase.CreateFolder( networksPath, "Connections" );
			*/
        return networksPath;
    }
}
