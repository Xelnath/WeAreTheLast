using System;
using System.Linq;
using System.Reflection;
using Atomech;
using DryIoc;
using Sini.Unity;
using UnityEditor;
using UnityEngine;

public class WindowsDockingPlugin 
{

	public void PreBind()
	{
	}

	public void Bind( IContainer _ )
	{
		_.Register<IWindowsDockingService, WindowsDockingService>();
	}

	public bool Enabled
	{
		get { return true; }
	}

	public string Title
	{
		get { return "Windows Docking"; }
	}

	public int Priority
	{
		get { return 0; }
	}
}

/// <summary>
/// Should only use Window for now.
/// </summary>
public enum DropType
{
	Tab,
	Pane,
	Window,
}

/// <summary>
/// Defines where to dock window relative to the target
/// </summary>
[Flags]
public enum DockType
{
	None = 0,
	Left = 1,
	Bottom = 2,
	Top = 4,
	Right = 8,
}

/// Look into Dry IOC and see if its apprporiate for this project
public interface IWindowsDockingService
{
	void Dock( EditorWindow window, EditorWindow target, DockType dockType, DropType dropType );
}


public class WindowsDockingService : IWindowsDockingService
	{
		private static Assembly m_editorAssembly;

		public void Dock( EditorWindow window, EditorWindow target, DockType dockType, DropType dropType = DropType.Window )
		{
			var reflectedTarget = new EditorWindowReflected(target);
			var host = reflectedTarget.Parent.Parent;
			var reflectedWindow = new EditorWindowReflected(window);


			var index = host.GetIndexOf(reflectedTarget.Parent.Value);
			if ( index == -1 )
			{
				index = 0;
			}
			var dropInfoExtra = ExtraDropInfoReflected.Create(false, dockType, index);


			var posRect = new Rect().WithSize(100,100);
			switch ( dockType )
			{
				case DockType.None:
					break;
				case DockType.Left:
					posRect.InnerAlignWithCenterLeft( target.position );
					break;
				case DockType.Bottom:
					posRect.InnerAlignWithBottomCenter( target.position );
					break;
				case DockType.Top:
					posRect.InnerAlignWithUpperCenter( target.position );
					break;
				case DockType.Right:
					posRect.InnerAlignWithCenterRight( target.position );
					break;
				default:
					throw new ArgumentOutOfRangeException( "dockType", dockType, null );
			}

			var dropInfo = DropInfoReflected.Create(host.Value, posRect, dropInfoExtra, dropType);
			DockAreaReflected.SOriginalDragSource = reflectedWindow.Parent.Value;
			host.PeformDrop( window, dropInfo, Vector2.zero );
			DockAreaReflected.ResetDragVars();

			target.Repaint();
			window.Repaint();

		}

		public static Assembly EditorAssembly
		{
			get { return m_editorAssembly ?? (m_editorAssembly = Assembly.GetAssembly( typeof( Editor ) )); }
		}

		internal class EditorWindowReflected
		{
			private readonly EditorWindow m_editorWindow;
			private static FieldInfo m_parentField;

			private static FieldInfo ParentField
			{
				get
				{
					if ( m_parentField == null )
					{
						m_parentField = typeof( EditorWindow ).GetField( "m_Parent", BindingFlags.Instance | BindingFlags.NonPublic );
					}
					return m_parentField;
				}
			}

			public EditorWindowReflected( EditorWindow editorWindow )
			{
				if ( editorWindow == null ) throw new Exception( "EditorWindow is null" );
				m_editorWindow = editorWindow;
			}

			public ViewReflected Parent
			{
				get { return new ViewReflected( ParentField.GetValue( m_editorWindow ) ); }
			}

		}

		internal class ContainerWindowReflected
		{
			private readonly object m_containerWindow;
			private static PropertyInfo m_rootViewProperty;

			private static PropertyInfo RootViewProperty
			{
				get
				{
					if ( m_rootViewProperty == null )
					{
						m_rootViewProperty = EditorAssembly.SearchType( "ContainerWindow" ).GetProperty( "rootView", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public );
					}
					return m_rootViewProperty;
				}
			}

			public ContainerWindowReflected( object containerWindow )
			{
				if ( containerWindow == null ) throw new Exception( "Container Window is null" );
				m_containerWindow = containerWindow;
			}

			public ViewReflected RootView
			{
				get
				{
					return new ViewReflected( RootViewProperty.GetValue( m_containerWindow, null ) );
				}
			}
		}


		internal class ViewReflected
		{
			private readonly object m_view;
			private static FieldInfo m_windowField;
			private static MethodInfo m_performDropMethod;
			private static PropertyInfo m_childrenProperty;
			private static PropertyInfo m_parentProperty;

			private static FieldInfo WindowField
			{
				get
				{
					if ( m_windowField == null )
					{
						m_windowField = EditorAssembly.SearchType( "View" ).GetField( "m_Window", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy );
					}
					return m_windowField;
				}
			}

			private static PropertyInfo ChildrenProperty
			{
				get
				{
					if ( m_childrenProperty == null )
					{
						m_childrenProperty = EditorAssembly.SearchType( "View" ).GetProperty( "children", BindingFlags.Instance | BindingFlags.Public );
					}
					return m_childrenProperty;
				}
			}

			private static PropertyInfo ParentProperty
			{
				get
				{
					if ( m_parentProperty == null )
					{
						m_parentProperty = EditorAssembly.SearchType( "View" ).GetProperty( "parent", BindingFlags.Instance | BindingFlags.Public );
					}
					return m_parentProperty;
				}
			}

			private static MethodInfo PerformDropMethod
			{
				get
				{
					if ( m_performDropMethod == null )
					{
						//    bool PerformDrop(EditorWindow w, DropInfo dropInfo, Vector2 screenPos);
						m_performDropMethod = EditorAssembly.SearchType( "IDropArea" ).GetMethod( "PerformDrop" );
					}
					return m_performDropMethod;
				}
			}

			public ViewReflected( object view )
			{
				m_view = view;
			}

			public ContainerWindowReflected Window
			{
				get
				{
					return new ContainerWindowReflected( WindowField.GetValue( m_view ) );
				}
			}

			public ViewReflected Parent
			{
				get
				{
					return new ViewReflected( ParentProperty.GetValue( m_view, null ) );
				}
			}


			public void PeformDrop( EditorWindow w, object dropInfo, Vector2 screenpos )
			{
				PerformDropMethod.Invoke( m_view, new[] { w, dropInfo, screenpos } );
			}

			public int GetIndexOf( object view )
			{
				var children = ChildrenProperty.GetValue(m_view,null) as Array;
				var ndx = 0;
				if ( children != null )
				{
					foreach ( var child in children )
					{
						if ( child == view )
						{
							return ndx;
						}
						ndx++;
					}
				}
				return -1;
			}

			public object Value
			{
				get { return m_view; }
			}

		}


		internal static class DropInfoReflected
		{
			private static Type m_dropInfoDropType;
			private static Type m_dropInfoType;
			private static FieldInfo m_userDataField;
			private static FieldInfo m_typeField;
			private static FieldInfo m_rectField;

			public static Type DropInfoDropType
			{
				get
				{
					if ( m_dropInfoDropType == null )
					{
						m_dropInfoDropType = DropInfoType.GetNestedTypes( BindingFlags.NonPublic ).FirstOrDefault( t => t.Name == "Type" );
					}
					return m_dropInfoDropType;
				}
			}

			public static Type DropInfoType
			{
				get
				{
					if ( m_dropInfoType == null )
					{
						m_dropInfoType = EditorAssembly.SearchType( "DropInfo" );
					}
					return m_dropInfoType;
				}
			}

			public static FieldInfo UserDataField
			{
				get
				{
					if ( m_typeField == null )
					{
						m_typeField = DropInfoType.GetField( "userData", BindingFlags.Public | BindingFlags.Instance );
					}
					return m_typeField;
				}
			}
			public static FieldInfo TypeField
			{
				get
				{
					if ( m_userDataField == null )
					{
						m_userDataField = DropInfoType.GetField( "type", BindingFlags.Public | BindingFlags.Instance );
					}
					return m_userDataField;
				}
			}
			public static FieldInfo RectField
			{
				get
				{
					if ( m_rectField == null )
					{
						m_rectField = DropInfoType.GetField( "rect", BindingFlags.Public | BindingFlags.Instance );
					}
					return m_rectField;
				}
			}

			public static object Create( object source, Rect rect, object userData = null, DropType type = DropType.Window )
			{
				var instance = Activator.CreateInstance(DropInfoType, new[] {source});
				UserDataField.SetValue( instance, userData );
				TypeField.SetValue( instance, Enum.ToObject( DropInfoDropType, (int) type ) );
				RectField.SetValue( instance, rect );
				return instance;
			}



		}

		internal static class ExtraDropInfoReflected
		{
			private static Type m_extraDropInfoType;
			private static Type m_viewEdgeType;

			public static Type ExtraDropInfoType
			{
				get
				{
					if ( m_extraDropInfoType == null )
					{
						m_extraDropInfoType = EditorAssembly.SearchType( "ExtraDropInfo" );
					}
					return m_extraDropInfoType;
				}
			}
			public static Type ViewEdgeType
			{
				get
				{
					if ( m_viewEdgeType == null )
					{
						m_viewEdgeType = EditorAssembly.SearchType( "ViewEdge" );
					}
					return m_viewEdgeType;
				}
			}

			public static object Create( bool rootWindow, DockType edge, int index )
			{
				return Activator.CreateInstance( ExtraDropInfoType, new[] { rootWindow, Enum.ToObject( ViewEdgeType, (int) edge ), index } );
			}




		}

		internal class DockAreaReflected
		{
			private static Type m_dockAreaType;
			private static FieldInfo m_sOriginalDragSourceField;
			private static MethodInfo m_resetDragVarsMethod;

			public static Type DockAreaType
			{
				get
				{
					if ( m_dockAreaType == null )
					{
						m_dockAreaType = EditorAssembly.SearchType( "DockArea" );
					}
					return m_dockAreaType;
				}
			}

			public static MethodInfo ResetDragVarsMethod
			{
				get
				{
					// private static void ResetDragVars()
					if ( m_resetDragVarsMethod == null )
					{
						m_resetDragVarsMethod = DockAreaType.GetMethod( "ResetDragVars", BindingFlags.NonPublic | BindingFlags.Static );
					}
					return m_resetDragVarsMethod;
				}
			}

			public static FieldInfo SOriginalDragSourceField
			{
				get
				{
					if ( m_sOriginalDragSourceField == null )
					{
						m_sOriginalDragSourceField = DockAreaType.GetField( "s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic );
					}
					return m_sOriginalDragSourceField;
				}
			}

			public static void ResetDragVars()
			{
				ResetDragVarsMethod.Invoke( null, null );
			}

			public static object SOriginalDragSource
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					SOriginalDragSourceField.SetValue( null, value );
				}
			}
		}

	}
