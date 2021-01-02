using UnityEngine;
using UnityEditor;
using Sirenix.Utilities.Editor;
using Quantum;
using Sini.Unity;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class SceneSelector : EditorWindow
{
	private Vector2 scrollPos;
	public Material mat;


	public bool ShowFavorites = true;
	public bool ShowBuild = true;
	public bool ShowAll = false;

	public void OnEnable()
	{
		var shader  = Shader.Find( "Hidden/Internal-Colored" );
		mat = new Material( shader );
	}

	[MenuItem( "Atomech/Scene Jump" )]
	static void Init()
	{
		var inspector = FindInspectorWindow();
		SceneSelector window = (SceneSelector)GetWindow<SceneSelector>("Scene Selector",false, inspector.GetType() );
		window.Show();

		IWindowsDockingService dockingService = new WindowsDockingService();
		dockingService.Dock( window, inspector, DockType.Right, DropType.Window );
	}

	public static EditorWindow FindInspectorWindow()
	{
		//var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
		var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.SceneView");
		var window = StaticUnityEditorHelper.GetWindowOfType(inspectorType, "Scene");
		if ( window == null )
			window = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;

		return window;
	}

	// Favorites List goes first!
	// TODO: Serialize this
	List<string> Labels = new List<string> { };
	List<string> Paths = new List<string> {  };

	public void OnGUI()
	{
		List<string> handledScenes = new List<string>();

		scrollPos = GUILayout.BeginScrollView( scrollPos );

		Color headerColor = Color.Lerp( GUI.color, PolymerColor.paper_blue_300.ToColor(), 0.5f);
		GUIHelper.PushColor( headerColor );
		var show = StaticUnityEditorHelper.BeginClickableBox( "Favorites", true, ShowFavorites );
		GUIHelper.PopColor();
		for ( int i = 0; i < Labels.Count && i < Paths.Count; ++i )
		{
			if ( handledScenes.Contains( Paths[i] ) ) continue;
			handledScenes.Add( Paths[i] );

			if ( ShowFavorites )
				DisplayBox( Labels[i], Paths[i], IsSceneOpen( Paths[i] ), IsSceneDirty( Paths[i] ) );
		}

		ShowFavorites = show;
		StaticUnityEditorHelper.EndClickableBox();

		GUIHelper.PushColor( headerColor );
		show = StaticUnityEditorHelper.BeginClickableBox( "BuildScenes", true, ShowBuild );
		GUIHelper.PopColor();
		for ( int i = 0; i < SceneManager.sceneCountInBuildSettings; i++ )
		{
			var path = SceneUtility.GetScenePathByBuildIndex(i);
			var sceneName = Path.GetFileNameWithoutExtension(path);

			if ( handledScenes.Contains( path ) ) continue;
			handledScenes.Add( path );

			if ( ShowBuild )
				DisplayBox( sceneName, path, IsSceneOpen( path ), IsSceneDirty( path ) );
		}
		ShowBuild = show;
		StaticUnityEditorHelper.EndClickableBox();

		GUIHelper.PushColor( headerColor );
		show = StaticUnityEditorHelper.BeginClickableBox( "Editor Scenes", true, ShowAll );
		GUIHelper.PopColor();
		var allScenes = StaticUnityEditorHelper.findAllScenePaths();
		for ( int i = 0; i < allScenes.Length; i++ )
		{
			var path = allScenes[i];
			var sceneName = Path.GetFileNameWithoutExtension(path);

			if ( handledScenes.Contains( path ) ) continue;
			handledScenes.Add( path );

			if ( ShowAll )
				DisplayBox( sceneName, path, IsSceneOpen( path ), IsSceneDirty( path ) );
		}
		ShowAll = show;
		StaticUnityEditorHelper.EndClickableBox();

		GUILayout.EndScrollView();
	}

	public bool IsSceneOpen( string scenePath )
	{
		for ( int i = 0; i < EditorSceneManager.sceneCount; ++i )
		{
			var scene  = EditorSceneManager.GetSceneAt( i );

			if ( scene.path.ToLowerInvariant() == scenePath.ToLowerInvariant() )
				return true;

		}
		return false;
	}
	public bool IsSceneDirty( string scenePath )
	{
		for ( int i = 0; i < EditorSceneManager.sceneCount; ++i )
		{
			var scene  = EditorSceneManager.GetSceneAt( i );

			if ( scene.path.ToLowerInvariant() == scenePath.ToLowerInvariant() )
				return scene.isDirty;

		}
		return false;
	}

	public void DisplayBox(string sceneName, string scenePath, bool isCurrentScene, bool isSceneDirty)
	{
		string name = string.Format("{0}{1}", sceneName, isSceneDirty ? "*" : "" );
		bool labelIsCentered = true;
		var clickRect = SirenixEditorGUI.BeginBox( name, labelIsCentered );

		Event current = Event.current;
		Rect clickArea = clickRect.WithHeight(20).InnerAlignWithUpperCenter(clickRect);

		if ( isCurrentScene )
		{
			var selectedColor = Color.yellow;
			selectedColor.a = 0.25f;
			EditorGUI.DrawRect( clickArea, selectedColor );
		}

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
				{
					if ( isCurrentScene )
					{
						Debug.Log( "Scene already open!" );
					}
					else
					{
						TryOpenScene( scenePath );
					}
				}
			}
		}

		SirenixEditorGUI.EndBox();
	}

	public void TryOpenScene(string scenePath)
	{
		for ( int i = 0; i < EditorSceneManager.sceneCount; ++i )
		{
			var scene  = EditorSceneManager.GetSceneAt( i );

			if ( scene.isDirty )
			{
				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
			}
		}

		if ( Event.current.shift )
			EditorSceneManager.OpenScene( scenePath, OpenSceneMode.Additive );
		else
			EditorSceneManager.OpenScene( scenePath, OpenSceneMode.Single );

	}

	public void OnInspectorUpdate()
	{
		this.Repaint();
	}
}
