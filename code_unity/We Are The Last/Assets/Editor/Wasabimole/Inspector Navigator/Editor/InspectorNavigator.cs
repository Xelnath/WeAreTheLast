#define FULL_VERSION

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Wasabimole.Core;
using UnityEditor.SceneManagement;
using UnityEngine.Serialization;

// ---------------------------------------------------------------------------------------------------------------------------
// Inspector Navigator - © 2014, 2015 Wasabimole http://wasabimole.com
// ---------------------------------------------------------------------------------------------------------------------------
// HOW TO - INSTALLING INSPECTOR NAVIGATOR:
//
// - Import package into project
// - Then click on:
//      Window -> Inspector Navigator -> Open Inspector Navigator Window
//
// The window will appear grouped with the Inspector by default, in a different tab. We recommend then to drag the 
// Inspector window just below the Inspector Navigator, and then adjust the Navigator's window height to a minimum.
// ---------------------------------------------------------------------------------------------------------------------------
// [NOTE] The source is intended to be used as is. Do NOT compile it into a DLL. We can't give support to modified versions.
// ---------------------------------------------------------------------------------------------------------------------------
// HOW TO - USING INSPECTOR NAVIGATOR:
//
// - Use the back '<' and forward '>' buttons to navigate to recent inspectors
// - Hotkeys [PC]:  Ctrl + Left/Right 
// - Hotkeys [Mac]: Alt + Cmd + Left/Right
// - Click on the breadcrumb bar to jump to any object
// - Click on the padlock icon to lock the tool to current objects
// - Drag and drop objects from the breadcrumb bar
// - To delete a breadcrumb, drag it to the Remove box
// - Access "Help and Options" from the menu to edit tool preferences
//
// [NOTE] Hotkeys can be changed by editing Wasabimole/Inspector Navigator/Editor/KeyBindings.cs
// ---------------------------------------------------------------------------------------------------------------------------
// INSPECTOR NAVIGATOR - VERSION HISTORY
//
// 1.23 Alignment update
// - New option to set breadcrumb bar horizontal alignment preference
// - New Asset Store Inspector filter
// - Fixed breadcrumbs objects leaking when breadcrumbs were not serialized
// - Keep assets in breadcrumbs when switching scenes and breadcrumbs are not serialized
// - Use shared Wasabimole.Core.UpdateNotifications library
// - Other small bug fixes
//
// 1.22 Disable serialization update
// - Serialization of breadcrumbs on scenes can now be disabled
// - Dialog for clearing project scenes when checking serialization off
// - Optimized tool start-up times when opening up a scene
// - Fixed problem where breadcrumbs disappeared when going into play mode
// - Unselecting an object no longer marks the scene as changed
// - Changed default object queue length to 64
// - Other small bug fixes
// 
// 1.20 Strip breadcrumbs update
// - InspectorBreadcrumbs no longer included in build on Unity 5 (using new HideFlags.DontSaveInBuild)
// - New menu option to delete inspector breadcrumbs from all project scenes (so they can be removed before performing a build on Unity 4.X)
// - InspectorBreadcrumbs are no longer created again right after being deleted (only after a new object/asset selection)
// - Do not create InspectorBreadcrumbs when selecting any filtered object
// - Upon load scene, do not automatically center camera on last selected object
// - New Scripts & TextAssets filter type, disabled by default
// - New ProjectSettings filter type, disabled by default
// - Removed all filtered objects from breadcrumbs on load scene
// - Modifying object filters has now immediate effect
// - InspectorBreadcrumbs object is now filtered, and no longer appears in the breadcrumbs bar
// - Warning before enabling project settings tracking on Unity 4.X
// - New menu option to check for new user notifications
// - Other small bug fixes
//
// 1.18 Keys & colors update
// - New button in options to define the hotkeys
// - Moved hot-key definitions to KeyBindings.cs
// - Option to set the text color for different object types
// - Fixed breadcrumbs not being properly removed
// - Fixed changing InspectorBreadcrumbs visibility
// - Fixed visual glitches on play mode
// - Other small bug fixes
//
// 1.16 Remove breadcrumbs update
// - Allow removing breadcrumbs by dragging them into the "Remove" box
// - Option to remove and not track unnamed objs
// - Fixed issue with lost notification messages
// - Remove any duplicate inspector breadcrumbs scene objects
// - Remove inspector breadcrumbs from scene when closing the tool
// - Allow deleting by hand InspectorBreadcrumbs object
// - Other small bug fixes
//
// 1.15 Unity 5 Hotfix [Must delete previous version first]
// - Fixed error GameObject (named 'BreadCrumbs') references runtime script in scene file. Fixing!​
// - Restructured project folders, now under Wasabimole/Inspector Navigator (must delete old Editor/InspectorNavigator.cs and Editor/NotificationCenter.cs files!)
// - Added option to show breadcrumbs object in the scene
// - Other small bug fixes
// 
// 1.14 Drag and drop update
// - Drag and drop breadcrumbs to any other Unity window or field
// - Set minimum window size to match the width of the 2 arrows
// - Selecting a filtered object now properly unselects breadcrumb
// - Added Wasabimle logo to help and options window
// - Used base64 for resource images
// - Other small bug fixes
//
// 1.11 Bug fixes update
// - Several small bug fixes
// - Removed compilation warnings
// - Option to check for plugin updates
// - Option to show other notifications
//
// 1.10 Big update
// - Restore previous scene camera when navigating back to an object
// - New improved breadcrumbs system and serialization method
// - Object breadcrumbs are now local to every scene
// - Optimized code for faster OnGUI calls
// - Option to filter which inspectors to track (scene objects, assets, folders, scenes)
// - Option to remove all duplicated objects
// - Option to set maximum number of enqueued objects
// - Option to mark the scene as changed or not on new selections
// - Option to review the plugin on the Asset Store
// - Other small bug fixes
//
// 1.07 Breadcrumb++ update
// - Improved breadcrumb bar behaviour
// - New Help and Options window
// - New tool padlock to lock to current objects
// - Option to set max label width
// - Option to clear or insert when selecting new objects
// - Option to remove duplicated objects when locked
// - Option to choose scene camera behaviour
// - Fixed default hotkeys on Mac to Alt + Cmd + Left/Right
// - Other small bug fixes
// 
// 1.03 Hotkeys update
// - Added Inspector Navigator submenu + hotkeys Ctrl/Cmd + Left/Right
// - Limited queue size
// - Handle Undo/Redo operations better
// - Handle inspector lock states
//
// 1.02 First public release
// - Small bug fixes
//
// 1.00 Initial Release
// – Back and Forward buttons navigate to recent object inspectors
// – Breadcrumb bar shows recent objects and allows click to jump to them
// – Inspector history is serialized when closing and opening Unity
// ---------------------------------------------------------------------------------------------------------------------------
// Thank you for choosing this extension, we sincerely hope you like it!
//
// Please send your feedback and suggestions to mailto://contact@wasabimole.com
// ---------------------------------------------------------------------------------------------------------------------------

namespace Wasabimole.InspectorNavigator.Editor
{
    public enum CameraBehavior
    {
        DoNotChange,
        AutoFrameSelected,
        RestorePreviousCamera
    }

    public enum DuplicatesBehavior
    {
        LeaveDuplicates,
        RemoveWhenLocked,
        RemoveAllDuplicates
    }

    public enum BarAlignment
    {
        Right,
        Center,
        Left
    }

    public class InspectorNavigator : EditorWindow, IHasCustomMenu
    {
        public const int CurrentVersion = 123;

        private static InspectorNavigator _instance;
        public static InspectorNavigator Instance { get { if (_instance == null) OpenWindow(); return _instance; } }

        private static GUIStyle _labelStyle;
        private static GUIStyle _deleteLabelStyle;
        private static Color _deleteLabelStyleColor;
        private static GUIStyle _opaqueBoxStyle;
        private static Texture2D _backBtn;
        private static Texture2D _forwardBtn;
        private static GUIContent _gc = new GUIContent();
        private static Texture2D _grayLabel;
        private static Texture2D _greenLabel;
        private static Texture2D _yellowLabel;
        private static Texture2D _redLabel;

        private InspectorBreadcrumbs _breadcrumbs;
        public InspectorBreadcrumbs Breadcrumbs { get { return GetInspectorBreadcrumbs(); } }

        private InspectorBreadcrumbs GetInspectorBreadcrumbs()
        {
            if (_breadcrumbs == null)
            {
                var crumbs = AssetDatabaseHelper.FindAssetsByType<InspectorBreadcrumbs>();
                _breadcrumbs = crumbs[0];
                if (_breadcrumbs == null)
                {
                    _breadcrumbs = ScriptableObject.CreateInstance<InspectorBreadcrumbs>();
                    _breadcrumbs.DataVersion = CurrentVersion;
                }
            }
            return _breadcrumbs;
        }


        private List<ObjectReference> Back { get { return Breadcrumbs.Back; } set { Breadcrumbs.Back = value; } }
        private List<ObjectReference> Forward { get { return Breadcrumbs.Forward; } set { Breadcrumbs.Forward = value; } }
        private ObjectReference CurrentSelection { get { return Breadcrumbs.CurrentSelection; } set { Breadcrumbs.CurrentSelection = value; } }

        [FormerlySerializedAs("ConfigurationDataVersion")] [SerializeField]
        public int configurationDataVersion = 100;
        [FormerlySerializedAs("Locked")] [SerializeField]
        public bool locked = false;

#if FULL_VERSION
        [FormerlySerializedAs("MaxLabelWidth")] [SerializeField]
        public int maxLabelWidth = 0;
        [FormerlySerializedAs("InsertNewObjects")] [SerializeField]
        public bool insertNewObjects = false;
        [FormerlySerializedAs("TrackObjects")] [SerializeField]
        public bool trackObjects = true;
        [FormerlySerializedAs("TrackAssets")] [SerializeField]
        public bool trackAssets = true;
        [FormerlySerializedAs("TrackFolders")] [SerializeField]
        public bool trackFolders = false;
        [FormerlySerializedAs("TrackScenes")] [SerializeField]
        public bool trackScenes = false;
        [FormerlySerializedAs("TrackTextAssets")] [SerializeField]
        public bool trackTextAssets = false;
        [FormerlySerializedAs("TrackProjectSettings")] [SerializeField]
        public bool trackProjectSettings = false;
        [FormerlySerializedAs("TrackAssetStoreInspector")] [SerializeField]
        public bool trackAssetStoreInspector = false;
        [FormerlySerializedAs("ForceDirty")] [SerializeField]
        public bool forceDirty = false;
        [FormerlySerializedAs("MaxEnqueuedObjects")] [SerializeField]
        public int maxEnqueuedObjects = 64;
        [FormerlySerializedAs("CameraBehavior")] [SerializeField]
        public CameraBehavior cameraBehavior = CameraBehavior.RestorePreviousCamera;
        [FormerlySerializedAs("DuplicatesBehavior")] [SerializeField]
        public DuplicatesBehavior duplicatesBehavior = DuplicatesBehavior.RemoveWhenLocked;
        [FormerlySerializedAs("CheckForUpdates")] [SerializeField]
        public bool checkForUpdates = true;
        [FormerlySerializedAs("OtherNotifications")] [SerializeField]
        public bool otherNotifications = true;
        [FormerlySerializedAs("ShowBreadcrumbsObject")] [SerializeField]
        public bool showBreadcrumbsObject = false;
        [FormerlySerializedAs("RemoveUnnamedObjects")] [SerializeField]
        public bool removeUnnamedObjects = false;
        [FormerlySerializedAs("ColorInstance")] [SerializeField]
        public Color colorInstance = Color.white;
        [FormerlySerializedAs("ColorAsset")] [SerializeField]
        public Color colorAsset = Color.white;
        [FormerlySerializedAs("ColorFolder")] [SerializeField]
        public Color colorFolder = Color.white;
        [FormerlySerializedAs("ColorScene")] [SerializeField]
        public Color colorScene = Color.white;
        [FormerlySerializedAs("ColorTextAssets")] [SerializeField]
        public Color colorTextAssets = Color.white;
        [FormerlySerializedAs("ColorProjectSettings")] [SerializeField]
        public Color colorProjectSettings = Color.white;
        [FormerlySerializedAs("ColorAssetStoreInspector")] [SerializeField]
        public Color colorAssetStoreInspector = Color.white;
        [FormerlySerializedAs("SerializeBreadcrumbs")] [SerializeField]
        public bool serializeBreadcrumbs = true;
        [FormerlySerializedAs("BarAlignment")] [SerializeField]
        public BarAlignment barAlignment = BarAlignment.Right;

        private ObjectReference _lastSelection = null;
#else
        int MaxEnqueuedObjects = 64;
#endif

        private bool _areAllInspectorsLocked;
        private bool _wasLocked = false;
        private bool _isDragging = false;

        [FormerlySerializedAs("UpdateNotifications")] [SerializeField]
        public UpdateNotifications updateNotifications;

        private IEnumerable _inspectorList;

        private GUIStyle _lockButtonStyle;

        private Rect _rectLabels, _rectRightCut, _rectLeftCut;

        private float _centerX, _labelOffset;
        private float _maxWidth, _currentWidthHalf;
        private string _lastScene = "";

        [MenuItem("Window/Inspector Navigator/Open Inspector Navigator Window")]
        public static void OpenWindow()
        {
            var ins = GetWindow<InspectorNavigator>("Insp.Navigator", false, new System.Type[] { GetInspectorWindowType() });
            ins.minSize = new Vector2(84, 20);
            if (ins != null) _instance = ins;
        }

#if UNITY_EDITOR_OSX
        [MenuItem("Window/Inspector Navigator/Back " + KeyBindings.BackMac)]
#else
        [MenuItem("Window/Inspector Navigator/Back " + KeyBindings.BackPC)]
#endif
        public static void DoBackCommand()
        {
            Instance.UnlockFirstInspector();
            Instance.updateNotifications.AddUsage();
            if (Instance.CurrentSelection != null) Instance.UpdateCurrentSelection();
            if (Instance.DoBack())
            {
                if (Instance.CurrentSelection != null) Instance.RestorePreviousCamera();
                Instance.Repaint();
            }
        }

#if UNITY_EDITOR_OSX
        [MenuItem("Window/Inspector Navigator/Forward " + KeyBindings.ForwardMac)]
#else
        [MenuItem("Window/Inspector Navigator/Forward " + KeyBindings.ForwardPC)]
#endif
        public static void DoForwardCommand()
        {
            Instance.UnlockFirstInspector();
            Instance.updateNotifications.AddUsage();
            if (Instance.CurrentSelection != null) Instance.UpdateCurrentSelection();
            if (Instance.DoForward())
            {
                if (Instance.CurrentSelection != null) Instance.RestorePreviousCamera();
                Instance.Repaint();
            }
        }

        [MenuItem("Window/Inspector Navigator/Check for new user notifications")]
        public static void CheckForNewUserNotifications()
        {
            Instance.updateNotifications.ForceGetNotification();
        }

        [MenuItem("Window/Inspector Navigator/Show last notification again")]
        public static void ShowLastNotification()
        {
            Instance.updateNotifications.ShowPreviousNotification();
        }

        [MenuItem("Window/Inspector Navigator/Show last notification again", true)]
        public static bool HasLastNotification()
        {
            return Instance.updateNotifications.HasPreviousNotification;
        }

        public static IEnumerable<GameObject> SceneRoots()
        {
            var prop = new HierarchyProperty(HierarchyType.GameObjects);
            var expanded = new int[0];
            while (prop.Next(expanded))
            {
                yield return prop.pptrValue as GameObject;
            }
        }

        [MenuItem("Window/Inspector Navigator/Help and Options ...")]
        public static void ShowHelpAndOptions()
        {
            GetWindowWithRect<HelpAndOptions>(new Rect(0, 0, HelpAndOptions.Width, HelpAndOptions.Height), true, "Help and Options", true);
        }

        private static System.Type GetInspectorWindowType()
        {
            return typeof(EditorGUI).Assembly.GetType("UnityEditor.InspectorWindow");
        }

        private void OnEnable()
        {
#if FULL_VERSION
            updateNotifications = new UpdateNotifications(CurrentVersion, "Inspector Navigator", 26181, Repaint, 0x2BC, checkForUpdates, otherNotifications, false);
#else
            NotificationCenter = new NotificationCenter(CurrentVersion, "Inspector Navigator", 26181, Repaint, 0x220, 0x80);
#endif
            minSize = maxSize = new Vector2(300, 22);
            GetInspectorList();
        }

        private void OnDestroy()
        {
        }

        private void ShowButton(Rect position)
        {
            if (_lockButtonStyle == null)
                _lockButtonStyle = "IN LockButton";
            locked = GUI.Toggle(position, locked, GUIContent.none, _lockButtonStyle);
        }

        private float GetLabelWidthFromInstance(ObjectReference obj)
        {
            if (obj == null || obj.OReference == null) return 0;
            _gc.text = obj.OReference.name;
            if (string.IsNullOrEmpty(_gc.text)) _gc.text = "...";
            var w = _labelStyle.CalcSize(_gc).x;
#if FULL_VERSION
            if (maxLabelWidth > 0f && w > maxLabelWidth) w = maxLabelWidth;
#endif
            return w;
        }

        private bool DrawLabel(Event e, ObjectReference obj, float x, float w, Texture2D background)
        {
            if (obj == null || obj.OReference == null)
                return false;
            var text = obj.OReference.name;
            if (string.IsNullOrEmpty(text)) text = "...";
            _labelStyle.normal.background = background;
            _gc.text = text;
            _rectLabels.width = w;
            _rectLabels.x = x;
#if FULL_VERSION
            switch(obj.OType)
            {
                case ObjectType.Asset: _labelStyle.normal.textColor = colorAsset; break;
                case ObjectType.Instance: _labelStyle.normal.textColor = colorInstance; break;
                case ObjectType.Folder: _labelStyle.normal.textColor = colorFolder; break;
                case ObjectType.Scene: _labelStyle.normal.textColor = colorScene; break;
                case ObjectType.TextAssets: _labelStyle.normal.textColor = colorTextAssets; break;
                case ObjectType.ProjectSettings: _labelStyle.normal.textColor = colorProjectSettings; break;
                case ObjectType.AssetStoreAssetInspector: _labelStyle.normal.textColor = colorAssetStoreInspector; break;
                default: _labelStyle.normal.textColor = Color.white; break;
            }
            GUI.Label(_rectLabels, _gc, _labelStyle);
            _labelStyle.normal.textColor = Color.white;
#else
            GUI.Label(rectLabels, gc, LabelStyle);
#endif
            if (_rectLabels.Contains(e.mousePosition) && e.type == EventType.MouseDrag && e.mousePosition.x < _rectRightCut.x)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { obj.OReference };
                string assetPath = AssetDatabase.GetAssetPath(obj.OReference);
                if (assetPath.Length > 0) DragAndDrop.paths = new[] { assetPath.Length > 0 ? assetPath : null };
                DragAndDrop.StartDrag("Breadcrumb");
                DragAndDrop.SetGenericData("ObjectReference", obj);
                _isDragging = true;
                e.Use();
                return false;
            }
            return (e.type == EventType.MouseUp && _rectLabels.Contains(e.mousePosition) && e.mousePosition.x < _rectRightCut.x);
        }

        public void ClearDuplicates()
        {
            if (CurrentSelection == null || Back == null || Forward == null) return;
            var hash = new HashSet<Object>();
            var list = new List<ObjectReference>();
            hash.Add(null);
            if (CurrentSelection.OReference != null) hash.Add(CurrentSelection.OReference);
            foreach (var i in Back)
                if (!hash.Contains(i.OReference)) { hash.Add(i.OReference); list.Add(i); }
            var tmp = Back;
            Back = list;
            list = tmp;
            list.Clear();
            foreach (var i in Forward)
                if (!hash.Contains(i.OReference)) { hash.Add(i.OReference); list.Add(i); }
            Forward = list;
        }

        private readonly Object[] _emptyObjectArray = new Object[0];

        public void OnGUI()
        {
            Preloads();
            CheckIfAllInspectorsAreLocked();

            if (_breadcrumbs == null)
            {
                EditorGUILayout.BeginHorizontal();
                GetControlRect(EditorGUIUtility.currentViewWidth - 90);
                GUI.enabled = false;
                GUILayout.Button(_backBtn);
                GUILayout.Button(_forwardBtn);
                GUI.enabled = true;
                if (updateNotifications.HasNotification) DrawNotificationIcon(Event.current);
                EditorGUILayout.EndHorizontal();
                return;
            }

            var eventCurrent = Event.current;
            var eventType = eventCurrent.type;

#if FULL_VERSION
            if (locked && !_wasLocked && duplicatesBehavior == DuplicatesBehavior.RemoveWhenLocked) ClearDuplicates();
            if (removeUnnamedObjects && CurrentSelection != null && CurrentSelection.OReference != null && string.IsNullOrEmpty(CurrentSelection.OReference.name))
            {
                CurrentSelection = null;
                Repaint();
            }
#else
            if (Locked && !wasLocked) ClearDuplicates();
#endif

            _wasLocked = locked;

            if (eventType == EventType.MouseUp || eventType == EventType.MouseDown)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.paths = null;
                DragAndDrop.objectReferences = _emptyObjectArray;
            }
            else if (eventType == EventType.DragUpdated)
            {
                var data = DragAndDrop.GetGenericData("ObjectReference");
                if (data != null && data is ObjectReference && (ObjectReference)data != null) _isDragging = true;
            }
            else if (eventType == EventType.DragExited)
            {
                _isDragging = false;
            }

            EditorGUILayout.BeginHorizontal();

            if (eventType == EventType.Repaint)
            {
                _rectLabels = GetControlRect(EditorGUIUtility.currentViewWidth - 90);
                _rectLabels.y += 3f;
                _rectRightCut = _rectLabels;
                _rectRightCut.x += _rectLabels.width;
                _rectRightCut.width = 90f;
                _rectRightCut.height--;
                _rectLeftCut = _rectLabels;
                _rectLeftCut.x -= 4f;
                _rectLeftCut.width = 4f;
                _maxWidth = _rectLabels.width * 0.5f;
                _currentWidthHalf = CurrentSelection == null ? 0f : GetLabelWidthFromInstance(CurrentSelection) * 0.5f;
                ComputeLabelPositions(_maxWidth, _currentWidthHalf, out _centerX, out _labelOffset);
            }
            else GetControlRect(EditorGUIUtility.currentViewWidth - 90);

            if (eventType == EventType.Repaint || eventType == EventType.MouseUp || eventType == EventType.MouseDrag)
            {
                int iBack = 0, iForward = 0;
                DrawLabels(eventCurrent, ref iBack, ref iForward, _maxWidth, _currentWidthHalf, _centerX, _labelOffset);

                if (iForward != 0)
                {
                    updateNotifications.AddUsage();
                    if (CurrentSelection != null) UpdateCurrentSelection();
                    for (int i = 0; i < iForward; ++i)
                        DoForward();
                    if (CurrentSelection != null) RestorePreviousCamera();
                    Repaint();
                }
                else if (iBack != 0)
                {
                    updateNotifications.AddUsage();
                    if (CurrentSelection != null) UpdateCurrentSelection();
                    for (int i = 0; i < iBack; ++i)
                        DoBack();
                    if (CurrentSelection != null) RestorePreviousCamera();
                    Repaint();
                }
            }


            GUI.color = GetSkinBackgroundColor();
            GUI.DrawTexture(_rectRightCut, EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(_rectLeftCut, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            
            EditorGUILayout.BeginHorizontal(GUILayout.Width(80));

            if (_isDragging)
            {
                GUILayout.Label("Remove", _deleteLabelStyle);
                var rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(eventCurrent.mousePosition))
                {
                    _deleteLabelStyle.normal.textColor = Color.green;
                    if (eventType == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        eventCurrent.Use();
                    }
                    else if (eventType == EventType.DragPerform) 
                    {
                        var data = DragAndDrop.GetGenericData("ObjectReference");
                        if (data != null && data is ObjectReference && (ObjectReference)data != null)
                        {
                            var or = (ObjectReference)data;
                            if (CurrentSelection == or)
                            {
                                Instance.UpdateCurrentSelection();
                                Instance.CurrentSelection = null;
                            }
                            else
                            {
                                for (int i = Back.Count - 1; i >= 0; i--)
                                {
                                    var obj = Back[i];
                                    if (obj != or) continue;
                                    Back.RemoveAt(i);
                                }
                                for (int i = Forward.Count - 1; i >= 0; i--)
                                {
                                    var obj = Forward[i];
                                    if (obj != or) continue;
                                    Forward.RemoveAt(i);
                                }
                            }
                        }

                        _isDragging = false;
                        Repaint();
                    }
                }
                else
                {
                    _deleteLabelStyle.normal.textColor = _deleteLabelStyleColor;
                    if (eventType == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        eventCurrent.Use();
                    }
                }
                GUI.Label(rect, "Remove", _deleteLabelStyle);
                GUILayout.Label(string.Empty,GUILayout.Width(0));
            }
            else
            {
                if (eventType == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; 
                    eventCurrent.Use();
                }

                GUI.enabled = Back.Count > 0;
                if (EditorApplication.isPlayingOrWillChangePlaymode) GUI.color = _colorDarken;
                if (GUILayout.Button(_backBtn))
                {
                    UnlockFirstInspector();
                    updateNotifications.AddUsage();
                    if (CurrentSelection != null) UpdateCurrentSelection();
                    if (DoBack())
                        if (CurrentSelection != null) RestorePreviousCamera();
                }
                GUI.enabled = Forward.Count > 0;
                if (GUILayout.Button(_forwardBtn))
                {
                    UnlockFirstInspector();
                    updateNotifications.AddUsage();
                    if (CurrentSelection != null) UpdateCurrentSelection();
                    if (DoForward())
                        if (CurrentSelection != null) RestorePreviousCamera();
                }
                GUI.color = Color.white;
            }
            GUI.enabled = true;

            if (updateNotifications.HasNotification) DrawNotificationIcon(eventCurrent);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

#if FULL_VERSION
            if (cameraBehavior == CameraBehavior.AutoFrameSelected)
            {
                if (CurrentSelection != null && CurrentSelection != _lastSelection)
                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.FrameSelected();
                _lastSelection = CurrentSelection;
            }
#endif
        }

        public void RemoveFilteredObjects()
        {
            if (_breadcrumbs == null) return;

            bool removedAny = false;

            if (CurrentSelection != null && IsObjectTypeFiltered(CurrentSelection.OReference))
            {
                CurrentSelection = null;
                removedAny = true;
            }
            for (int i = Back.Count - 1; i >= 0; i--)
                if (IsObjectTypeFiltered(Back[i].OReference))
                {
                    Back.RemoveAt(i);
                    removedAny = true;
                }
            for (int i = Forward.Count - 1; i >= 0; i--)
                if (IsObjectTypeFiltered(Forward[i].OReference))
                {
                    Forward.RemoveAt(i);
                    removedAny = true;
                }

            if (removedAny) Repaint();
        }

        public void ReassignObjectTypes()
        {
            if (_breadcrumbs == null) return;
            if (CurrentSelection != null)
                CurrentSelection.OType = GetObjectType(CurrentSelection.OReference);
            for (int i = Back.Count - 1; i >= 0; i--)
                Back[i].OType = GetObjectType(Back[i].OReference);
            for (int i = Forward.Count - 1; i >= 0; i--)
                Forward[i].OType = GetObjectType(Forward[i].OReference);
        }

        private Color _skinBackgroundColor;
        private Color _colorDarken;
        private int _skinState = -1;

        private Color GetSkinBackgroundColor()
        {
            var state = Application.isPlaying ? 1 : 0;
            state += EditorGUIUtility.isProSkin ? 2 : 0;
            state += EditorApplication.isPlayingOrWillChangePlaymode ? 4 : 0;
            if (state == _skinState)
                return _skinBackgroundColor;
            else try
                {
                    var hostViewType = typeof(EditorGUI).Assembly.GetType("UnityEditor.HostView");
                    var kViewColor = (Color)hostViewType.GetField("kViewColor", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                    var kPlayModeDarken = hostViewType.GetField("kPlayModeDarken", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                    var prefColorType = typeof(EditorGUI).Assembly.GetType("UnityEditor.PrefColor");
                    _colorDarken = (Color)prefColorType.GetField("m_color", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(kPlayModeDarken);
                    var kDarkViewBackground = (Color)typeof(EditorGUIUtility).GetField("kDarkViewBackground", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                    var color = (!EditorGUIUtility.isProSkin) ? kViewColor : kDarkViewBackground;
                    _skinBackgroundColor = (!EditorApplication.isPlayingOrWillChangePlaymode) ? color : (color * _colorDarken);
                    _skinBackgroundColor.a = 255;
                }
                catch
                {
                    _colorDarken = Color.white;
                    _skinBackgroundColor = EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);
                }
            _skinState = state;
            return _skinBackgroundColor;
        }

        private void DrawLabels(Event e, ref int iBack, ref int iForward, float maxWidth, float currentWidthHalf, float centerX, float labelOffset)
        {
            if (CurrentSelection != null)
                if (DrawLabel(e, CurrentSelection, centerX + labelOffset - currentWidthHalf, currentWidthHalf * 2f, locked ? _yellowLabel : _greenLabel))
                    RestorePreviousCamera();

            var xl = currentWidthHalf;
            for (int i = Back.Count - 1; i >= 0; i--)
            {
                try
                {
                    var obj = Back[i];
                    if (obj == null || obj.OReference == null) throw new System.Exception();
#if FULL_VERSION
                    if (removeUnnamedObjects && obj.OReference.name.Length == 0) throw new System.Exception();
#endif
                    if (xl >= maxWidth + labelOffset) break;
                    var width = GetLabelWidthFromInstance(obj);
                    xl += width;
                    if (DrawLabel(e, Back[i], centerX + labelOffset - xl, width, _grayLabel))
                    {
                        UnlockFirstInspector();
                        iBack = Back.Count - i;
                    }
                }
                catch
                {
                    Back.RemoveAt(i);
                }
            }
            var xr = currentWidthHalf;
            for (int i = Forward.Count - 1; i >= 0; i--)
            {
                try
                {
                    var obj = Forward[i];
                    if (obj == null || obj.OReference == null) throw new System.Exception();
#if FULL_VERSION
                    if (removeUnnamedObjects && obj.OReference.name.Length == 0) throw new System.Exception();
#endif
                    if (xr >= maxWidth - labelOffset) break;
                    var width = GetLabelWidthFromInstance(obj);
                    if (DrawLabel(e, Forward[i], centerX + labelOffset + xr, width, _grayLabel))
                    {
                        UnlockFirstInspector();
                        iForward = Forward.Count - i;
                    }
                    xr += width;
                }
                catch
                {
                    Forward.RemoveAt(i);
                }
            }
        }

        private void ComputeLabelPositions(float maxWidth, float currentWidthHalf, out float centerX, out float labelOffset)
        {
            centerX = _rectLabels.x + maxWidth;
            labelOffset = 0f;
            var xl = currentWidthHalf;
            var xr = currentWidthHalf;
            int il = 1, ir = 1;
            do
            {
                if (Back.Count >= il && (xl <= xr || Forward.Count < ir))
                {
                    var obj = Back[Back.Count - il++];
                    if (obj == null || obj.OReference == null) { Back.RemoveAt(Back.Count - --il); continue; }
                    xl += GetLabelWidthFromInstance(obj);
                    if (xl > maxWidth && Forward.Count < ir && xr < maxWidth)
                    {
                        var freeSpace = maxWidth - xr;
                        var reqSpace = xl - maxWidth;
                        if (reqSpace < freeSpace)
                        {
                            labelOffset += reqSpace;
                            xl -= reqSpace;
                            xr += reqSpace;
                        }
                        else
                        {
                            labelOffset += freeSpace;
                            xl -= freeSpace;
                            xr += freeSpace;
                            break;
                        }
                    }
                }
                else if (Forward.Count >= ir)
                {
                    var obj = Forward[Forward.Count - ir++];
                    if (obj == null || obj.OReference == null) { Forward.RemoveAt(Forward.Count - --ir); continue; }
                    xr += GetLabelWidthFromInstance(obj);
                    if (xr > maxWidth && Back.Count < il && xl < maxWidth)
                    {
                        var freeSpace = maxWidth - xl;
                        var reqSpace = xr - maxWidth;
                        if (reqSpace < freeSpace)
                        {
                            labelOffset -= reqSpace;
                            xl += reqSpace;
                            xr -= reqSpace;
                        }
                        else
                        {
                            labelOffset -= freeSpace;
                            xl += freeSpace;
                            xr -= freeSpace;
                            break;
                        }
                    }
                }
                else break;
            } while (true);

#if FULL_VERSION
            switch (barAlignment)
            {
                case BarAlignment.Right:
                    if (xl <= maxWidth && xr <= maxWidth) labelOffset += maxWidth - xr;
                    break;
                case BarAlignment.Left:
                    if (xl <= maxWidth && xr <= maxWidth) labelOffset -= maxWidth - xl;
                break;
            }
#else
            if (xl <= maxWidth && xr <= maxWidth) labelOffset += maxWidth - xr;
#endif
        }

        private void AddCurrentToBack()
        {
            if (CurrentSelection == null) return;
            if (Back.Count == 0 || Back[Back.Count - 1] != CurrentSelection)
            {
                Back.Add(CurrentSelection);
                if (Back.Count > maxEnqueuedObjects)
                    Back.RemoveAt(0);
            }
        }

        private void AddCurrentToForward()
        {
            if (CurrentSelection == null) return;
            if (Forward.Count == 0 || Forward[Forward.Count - 1] != CurrentSelection)
            {
                Forward.Add(CurrentSelection);
                if (Back.Count > maxEnqueuedObjects)
                    Forward.RemoveAt(0);
            }
        }

        private bool DoForward()
        {
            if (_areAllInspectorsLocked) return false;
            if (Forward.Count > 0)
            {
                AddCurrentToBack();
                CurrentSelection = Forward[Forward.Count - 1];
                Forward.RemoveAt(Forward.Count - 1);
                return true;
            }
            return false;
        }

        private bool DoBack()
        {
            if (_areAllInspectorsLocked) return false;
            if (Back.Count > 0)
            {
                AddCurrentToForward();
                CurrentSelection = Back[Back.Count - 1];
                Back.RemoveAt(Back.Count - 1);
                return true;
            }
            return false;
        }

        private void DrawNotificationIcon(Event e)
        {
            _labelStyle.normal.background = updateNotifications.Blink ? _redLabel : _grayLabel;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.margin.top = 4;
            _gc.text = "\x21";
            GUILayout.Label(_gc, _labelStyle, GUILayout.Width(16));
            _labelStyle.fontStyle = FontStyle.Normal;
            if (e.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
                updateNotifications.AttendNotification();
        }

        public void LimitQueueSizes()
        {
            if (_breadcrumbs == null) return;
            if (Back.Count > maxEnqueuedObjects) Back.RemoveRange(0, Back.Count - maxEnqueuedObjects);
            if (Forward.Count > maxEnqueuedObjects) Forward.RemoveRange(0, Forward.Count - maxEnqueuedObjects);
        }

        private void OnLoadScene()
        {
#if FULL_VERSION
            if (!serializeBreadcrumbs)
            {
                if (Instance._breadcrumbs != null)
                {
                    LimitQueueSizes();
                    UnselectCurrent();

                    var list = new List<ObjectReference>();
                    foreach (var i in Back)
                        if (GetObjectType(i.OReference) != ObjectType.Instance) list.Add(i);
                    var tmp = Back;
                    Back = list;
                    list = tmp;
                    list.Clear();
                    foreach (var i in Forward)
                        if (GetObjectType(i.OReference) != ObjectType.Instance) list.Add(i);
                    Forward = list;
                }
            }
            else
            {
                LimitQueueSizes();
                UnselectCurrent();
            }

            if (configurationDataVersion < 110 && cameraBehavior == CameraBehavior.DoNotChange)
                cameraBehavior = CameraBehavior.RestorePreviousCamera;
            if (duplicatesBehavior == DuplicatesBehavior.RemoveAllDuplicates)
                ClearDuplicates();
#else
            LimitQueueSizes();
            UnselectCurrent();
#endif
            if (_breadcrumbs != null)
            {
                if (Breadcrumbs.DataVersion < CurrentVersion)
                {
                    if (Breadcrumbs.DataVersion < 120)
                    {
                        ReassignObjectTypes();
                        RemoveFilteredObjects();
                    }
                    Breadcrumbs.DataVersion = CurrentVersion;
                    EditorUtility.SetDirty(_breadcrumbs);
                }
            }
            configurationDataVersion = CurrentVersion;
        }

        private void OnHierarchyChange()
        {
            if (_breadcrumbs == null)
                GetInspectorBreadcrumbs();
#pragma warning disable 618

			if( EditorApplication.currentScene != _lastScene)
            {
                _lastScene = EditorApplication.currentScene;
                OnLoadScene();
            }
#pragma warning restore 618
			Repaint();
        }

        private void OnProjectChange()
        {
            Repaint();
        }

        private void OnSelectionChange()
        {
#pragma warning disable 618
			if( EditorApplication.currentScene != _lastScene) return;
#pragma warning restore 618
			var activeObject = Selection.activeObject;

            if (_breadcrumbs == null && (locked || activeObject == null)) return;
            
            if (CheckIfAllInspectorsAreLocked()) return;

            var type = activeObject == null? ObjectType.None : GetObjectType(activeObject);

            if (IsObjectTypeFiltered(activeObject, type)) activeObject = null;

            if (activeObject == null) // Unselect / Select a filtered type
            {
                UnselectCurrent();
                return;
            }
            
            if (CurrentSelection == null || CurrentSelection.OReference != activeObject) // New object selected
            {
                if (CurrentSelection != null) UpdateCurrentSelection();

                if (locked)
                {
                    AddCurrentToBack();
                    int i = 1, oi = 0;
                    do
                    {
                        if (Back.Count >= i && Back[Back.Count - i].OReference == activeObject) { oi = -i; break; }
                        if (Forward.Count >= i && Forward[Forward.Count - i].OReference == activeObject) { oi = i; break; }
                        i++;
                    } while (Back.Count >= i || Forward.Count >= i);
                    if (oi < 0)
                    {
                        while (oi++ < 0) DoBack();
                        CurrentSelection = CreateObjectReference(activeObject, type);
                    }
                    else if (oi > 0)
                    {
                        while (oi-- > 0) DoForward();
                        CurrentSelection = CreateObjectReference(activeObject, type);
                    }
                    else CurrentSelection = null;
                }
                else if (Back.Count > 0 && Back[Back.Count - 1].OReference == activeObject) // Undo/Back
                {
                    DoBack();
                    CurrentSelection = CreateObjectReference(activeObject, type);
                }
                else if (Forward.Count > 0 && Forward[Forward.Count - 1].OReference == activeObject) // Redo/Forward
                {
                    DoForward();
                    CurrentSelection = CreateObjectReference(activeObject, type);
                }
                else
                {
#if FULL_VERSION
                    ObjectReference or = null;
                    if (duplicatesBehavior == DuplicatesBehavior.RemoveAllDuplicates)
                    {
                        for (var n = Back.Count - 1; n >= 0; n--)
                            if (Back[n].OReference == activeObject)
                            {
                                if (or == null) or = Back[n];
                                Back.RemoveAt(n);
                            }
                        for (var n = Forward.Count - 1; n >= 0; n--)
                            if (Forward[n].OReference == activeObject)
                            {
                                if (or == null) or = Forward[n];
                                if (insertNewObjects) Forward.RemoveAt(n);
                            }
                    }
                    AddCurrentToBack();
                    if (!insertNewObjects) Forward.Clear();
                    CurrentSelection = or ?? CreateObjectReference(activeObject, type);
#else
					AddCurrentToBack();
                    Forward.Clear();
                    CurrentSelection= CreateObjectReference(activeObject, type);
#endif

                }
#if FULL_VERSION
                if (forceDirty) EditorUtility.SetDirty(_breadcrumbs);
#endif
                Repaint();
            }
        }

        private bool IsObjectTypeFiltered(Object obj)
        {
            return IsObjectTypeFiltered(obj, GetObjectType(obj));
        }

        private bool IsObjectTypeFiltered(Object obj, ObjectType type)
        {
            if (obj == null) return false;

#if FULL_VERSION
            if (removeUnnamedObjects && string.IsNullOrEmpty(obj.name)) return true;
            switch (type)
            {
                case ObjectType.InspectorBreadcrumbs: return true; 
                case ObjectType.Folder: return !trackFolders;
                case ObjectType.Instance: return !trackObjects;
                case ObjectType.Asset: return !trackAssets;
                case ObjectType.Scene: return !trackScenes;
                case ObjectType.TextAssets: return !trackTextAssets;
                case ObjectType.ProjectSettings: return !trackProjectSettings;
                case ObjectType.AssetStoreAssetInspector: return !trackAssetStoreInspector;
            }
#else
			if (type == ObjectType.Folder || type == ObjectType.Scene || type == ObjectType.ProjectSettings || type == ObjectType.InspectorBreadcrumbs || type == ObjectType.TextAssets || type == ObjectType.AssetStoreAssets) return true;
#endif

            return false;
        }

        private void UnselectCurrent()
        {
            if (_breadcrumbs != null)
            {
                if (CurrentSelection != null)
                {
                    AddCurrentToBack();
                    UpdateCurrentSelection();
                    CurrentSelection = null;
                    Repaint();
                }
            }
        }

        private static ObjectType GetObjectType(Object activeObject)
        {
            if (activeObject == null) return ObjectType.None;

            if ( Instance._breadcrumbs != null )
            {
                if ( activeObject == Instance._breadcrumbs ) return ObjectType.InspectorBreadcrumbs;
            }

            if (activeObject is GameObject)
            {

				PrefabAssetType pta = PrefabUtility.GetPrefabAssetType((GameObject) activeObject);
				PrefabInstanceStatus pis = PrefabUtility.GetPrefabInstanceStatus(activeObject);

				switch( pta ) {
					case PrefabAssetType.NotAPrefab:
					case PrefabAssetType.Regular:
					case PrefabAssetType.Variant:
					case PrefabAssetType.MissingAsset:
						return ObjectType.Instance;
				}

     //           PrefabType pt = PrefabUtility.GetPrefabType((GameObject)activeObject);
     //           if (pt == PrefabType.None || 
					//pt == PrefabType.DisconnectedModelPrefabInstance || 
					//pt == PrefabType.DisconnectedPrefabInstance ||
     //               pt == PrefabType.MissingPrefabInstance || 
					//pt == PrefabType.ModelPrefabInstance || 
					//pt == PrefabType.PrefabInstance) 
     //               return ObjectType.Instance;
            }

            if (activeObject as UnityEngine.TextAsset != null)
                return ObjectType.TextAssets;
            
            if (activeObject.ToString().Contains("UnityEngine.SceneAsset"))
                return ObjectType.Scene;

            var assetPath = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                if (activeObject.GetType().ToString().Contains("AssetStoreAssetInspector"))
                    return ObjectType.AssetStoreAssetInspector;
                else
                    return ObjectType.Asset;
            }
            if (assetPath.StartsWith("ProjectSettings/")) return ObjectType.ProjectSettings;
                
            try
            {
                System.IO.FileAttributes fileAttr = System.IO.File.GetAttributes(Application.dataPath + "/" + assetPath.Replace("Assets/", ""));
                if ((fileAttr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                    return ObjectType.Folder;
            }
            catch { }

            return ObjectType.Asset;
        }

        private void Update()
        {
            if (updateNotifications.Update(true)) Repaint();
        }

        private void Preloads()
        {
            if (_labelStyle == null)
            {
                minSize = maxSize = new Vector2(300, 22);
                _labelStyle = new GUIStyle()
                {
                    alignment = TextAnchor.UpperLeft,
                    border = new RectOffset(7, 7, 0, 0),
                    clipping = TextClipping.Clip,
                    fontSize = 0,
                    fixedHeight = 15,
                    margin = new RectOffset(3, 3, 0, 0),
                    padding = new RectOffset(6, 6, 0, 0),
                };
                _labelStyle.normal.textColor = Color.white;

                _deleteLabelStyle = new GUIStyle(EditorStyles.miniButtonMid) { margin = new RectOffset(0, 0, 4, 5) };
                _deleteLabelStyleColor = _deleteLabelStyle.normal.textColor;
            }

            if (_opaqueBoxStyle == null)
            {
                _opaqueBoxStyle = new GUIStyle();
                _opaqueBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
            }

            if (_grayLabel == null) _grayLabel = IconContent("sv_icon_name0");
            if (_greenLabel == null) _greenLabel = IconContent("sv_icon_name3");
            if (_yellowLabel == null) _yellowLabel = IconContent("sv_icon_name4");
            if (_redLabel == null) _redLabel = IconContent("sv_icon_name6");

            if (_forwardBtn == null || _backBtn == null)
            {
                _forwardBtn = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                if (!EditorGUIUtility.isProSkin)
                    _forwardBtn.LoadImage(System.Convert.FromBase64String(DataLight));
                else
                    _forwardBtn.LoadImage(System.Convert.FromBase64String(DataDark));
                _forwardBtn.Apply();
                _forwardBtn.hideFlags = HideFlags.HideAndDontSave;

                _backBtn = FlipTexture(_forwardBtn);
                _backBtn.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private Texture2D FlipTexture(Texture2D original)
        {
            var flipped = new Texture2D(original.width, original.height, original.format, false) { filterMode = original.filterMode };
            var w = original.width;
            var h = original.height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    flipped.SetPixel(w - x - 1, y, original.GetPixel(x, y));
            flipped.Apply();
            return flipped;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Lock"), locked, () => { locked = !locked; });
            menu.AddItem(new GUIContent("Check for new user notifications"), false, new GenericMenu.MenuFunction(() => { updateNotifications.ForceGetNotification(); }));
            if (updateNotifications.HasPreviousNotification)
                menu.AddItem(new GUIContent("Show last notification again"), false, new GenericMenu.MenuFunction(() => { updateNotifications.ShowPreviousNotification(); }));
            menu.AddItem(new GUIContent("Help and Options ..."), false, new GenericMenu.MenuFunction(() => GetWindowWithRect<HelpAndOptions>(new Rect(0, 0, HelpAndOptions.Width, HelpAndOptions.Height), true, "Help and Options", true)));
        }

        private const string DataDark = "iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAQAAAD8fJRsAAAAuUlEQVQY02P8z4AdMDHgAv8ZVMTm+VvwI4tdZvjPwMTA8F8kxGtTxTI3B0E0oxiZ/jNwCPkEr2+65sDAiCTxn4GR4T8DAwMTm3Tky/pJNhBJJgYGRgYGqBQjA4ekS9DkQDcOBgYWiAMYoWZMeVB9mOEWw28GBhaYUR/+rHky/+yVawyvGX5d/g/V8eb31hfzLpy/zPCS4QeSPxikGZwYFBjYUf3B+J9hFTMDM8Of//8YGWD2MTCEMQAAsmU4qA1aNcUAAAAASUVORK5CYII=";
        private const string DataLight = "iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAQAAAD8fJRsAAAAr0lEQVQY02P8z4AdMDHglngoluB/kh9Z0AAiwSiyzSukItLtgCCajv9MDAw/hPYERzTpOjAwotnByMDI8JftZaRMfa4NRBLN8l+SB4NyAvdyMDCwwIT+MzAyMDAkPeg4zHCL4TeSBP8fvydpZzWuMbxm+GXw/wJEQuC364u0C/qXGV4y/GBAGPFf+r/Tf4X/7P8ZYFCf4T8DCwND+AuG1wx//v9jZGBggASQOgMDAwDAYjipmcIucgAAAABJRU5ErkJggg==";

        private Texture2D IconContent(string name)
        {
            System.Reflection.MethodInfo mi = typeof(EditorGUIUtility).GetMethod("IconContent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(string) }, null);
            if (mi == null) mi = typeof(EditorGUIUtility).GetMethod("IconContent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new System.Type[] { typeof(string) }, null);
            return (Texture2D)((GUIContent)mi.Invoke(null, new object[] { name })).image;
        }

        private Rect GetControlRect(float width)
        {
            System.Reflection.MethodInfo mi = typeof(EditorGUILayout).GetMethod("GetControlRect", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(GUILayoutOption[]) }, null);
            if (mi == null) mi = typeof(EditorGUILayout).GetMethod("GetControlRect", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new System.Type[] { typeof(GUILayoutOption[]) }, null);
            return (Rect)mi.Invoke(null, new object[] { new GUILayoutOption[] { GUILayout.Width(width) } });
        }

        private void GetInspectorList()
        {
            try
            {
                _inspectorList = (IEnumerable)GetInspectorWindowType().GetMethod("GetInspectors", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            }
            catch { };
        }

        private bool CheckIfAllInspectorsAreLocked()
        {
            try
            {
                var enumerator = _inspectorList.GetEnumerator();
                while (enumerator.MoveNext())
                    if (!(bool)GetInspectorWindowType().GetProperty("isLocked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(enumerator.Current, null))
                        return _areAllInspectorsLocked = false;
                return _areAllInspectorsLocked = true;
            }
            catch { };
            return _areAllInspectorsLocked = false;
        }

        private void UnlockFirstInspector()
        {
            if (!_areAllInspectorsLocked) return;
            try
            {
                var enumerator = _inspectorList.GetEnumerator();
                enumerator.MoveNext();
                var firstInspectorWindow = enumerator.Current;
                GetInspectorWindowType().GetProperty("isLocked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).SetValue(firstInspectorWindow, false, null);
                _areAllInspectorsLocked = false;
            }
            catch { };
        }

        private ObjectReference CreateObjectReference(Object @object, ObjectType type)
        {
            var or = new ObjectReference();
            or.OReference = @object;
            or.OType = type;
            if (SceneView.lastActiveSceneView != null)
            {
                or.CPosition = SceneView.lastActiveSceneView.pivot;
                or.CRotation = SceneView.lastActiveSceneView.rotation;
                or.CSize = SceneView.lastActiveSceneView.size;
                or.COrtho = SceneView.lastActiveSceneView.orthographic;
            }
            return or;
        }

        private void RestorePreviousCamera()
        {
            Selection.activeObject = CurrentSelection.OReference;
#if FULL_VERSION
            if (cameraBehavior != CameraBehavior.RestorePreviousCamera) return;
#endif
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(CurrentSelection.CPosition, CurrentSelection.CRotation, CurrentSelection.CSize, CurrentSelection.COrtho, true);
        }

        private void UpdateCurrentSelection()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                CurrentSelection.CPosition = SceneView.lastActiveSceneView.pivot;
                CurrentSelection.CRotation = SceneView.lastActiveSceneView.rotation;
                CurrentSelection.CSize = SceneView.lastActiveSceneView.size;
                CurrentSelection.COrtho = SceneView.lastActiveSceneView.orthographic;
            }
        }
    }
}