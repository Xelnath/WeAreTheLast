using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using JetBrains.Annotations;
using Sini.Unity;
using GoogleSheetsForUnity;
using DryIoc;
using Atomech;

public class LocalizationRecordOverviewEditorWindow : EditorWindow
{
#pragma warning disable 414
	[NonSerialized]
	private bool m_isInitialized;
#pragma warning restore 414

    private LocalizationRecordSheetCache m_sheetCache;
    public LocalizationRecordSheetCache SheetCache
    {
        get {
            if ( m_sheetCache == null )
            {
                m_sheetCache = AssetDatabaseHelper.FindAssetsByType<LocalizationRecordSheetCache>().FirstOrDefault();
            }
            return m_sheetCache;
        }
    }

	List<LocalizationRecord> LocalizationRecords
    {
        get {
            return Kor.Kor.Instance.Container.Resolve<LocalizationRecordCacheService>().LocRecords;
        }
    }

	void OnEnable() {
	}

	void Update() {
		Repaint();
	}

	void FetchAllPages() {
		Dictionary<ConnectionData, List<string>> pages = new Dictionary<ConnectionData, List<string>>();

		ConnectionData defaultConnection;
		var def = AssetDatabaseHelper.FindAssetsByType<DriveConnection>();
		defaultConnection = def[0].connectionData;

		foreach ( var record in LocalizationRecords ) {

			var connectionData = record.connectionData ?? defaultConnection;

			if ( pages.ContainsKey(connectionData) == false ) {
				pages[ connectionData ] = new List<string>();
			}
			if ( pages[ connectionData ].Contains(record.SheetName) ) {
				continue;
			}

			pages[ connectionData ].Add(record.SheetName);
		}

		foreach ( var kvp in pages ) {
            LocalizationRecordHelper.SetupConnection(kvp.Key);
			Drive.responseCallback += HandleRemoteFetch;
			foreach ( string page in kvp.Value ) {
				Drive.GetTable(page, false);
			}
		}
	}

	public void HandleRemoteFetch(Drive.DataContainer dataContainer) {
		if ( dataContainer.objType != "" ) {

			LocalizationMetaData[] metas;
			LocalizedStringData[] strings;

			if ( dataContainer.QueryType == Drive.QueryType.getTable ) {
				string rawJSon = dataContainer.payload;
				UnityEngine.Debug.Log($"Data from Google Drive received. [{dataContainer.objType}]");

				// Parse from json to the desired object type.
				metas = JsonHelper.ArrayFromJson<LocalizationMetaData>(rawJSon);
				strings = JsonHelper.ArrayFromJson<LocalizedStringData>(rawJSon);

				for ( int i = 0; i < metas.Length; ++i ) {
					var meta = metas[ i ];
					var str = strings[ i ];

					if ( string.IsNullOrEmpty(meta.ID) ) continue; 

					foreach ( var record in LocalizationRecords ) {
						if ( record.SheetName == dataContainer.objType ) {
							if ( record.MetaDataLocal.ID == meta.ID ) {
                                LocalizationRecordHelper.UpdateRecord(record, meta, str);
							}
						}
					}
				}
			}
		}
	}
    public struct RectCache
    {
        public Rect toolbar;
        public Rect assetHeaderRect;
        public Rect uploadAllNewRect;
        public Rect fetchAllRect;
        public Rect acceptAllRect;
        public Rect differenceRect;
        public Rect pushRect;
    }

	public Vector2 m_scroll;
	void OnGUI() {
		var viewRect = new Rect(0, 0, position.width, position.height);
		if ( !m_isInitialized ) {
			//GUI.Label(viewRect, "This window is not initialized yet", EditorStyles.centeredGreyMiniLabel);
			//return;
		}

        if ( SheetCache == null )
        {
            GUI.Label(viewRect, "Create a sheet cache asset for better organization.", EditorStyles.centeredGreyMiniLabel);
        }

        var labelStyle = GUI.skin.button;

        RectCache cache = new RectCache();

        var UploadAllNew = new GUIContent("Upload All New");
        var FetchAll = new GUIContent("Fetch All");
        var AcceptAll = new GUIContent("Accept All");
        var KeepUnity = new GUIContent("Keep Unity Changes");
        var PushChanges = new GUIContent("Push Changes to Google");

        cache.toolbar = viewRect.WithHeight(16f);
        cache.assetHeaderRect = cache.toolbar.WithWidth(400);
        cache.uploadAllNewRect = cache.assetHeaderRect.RightOf(cache.assetHeaderRect).WithWidth(labelStyle.CalcSize(UploadAllNew).x);
        cache.fetchAllRect = cache.uploadAllNewRect.RightOf(cache.uploadAllNewRect).WithWidth(labelStyle.CalcSize(FetchAll).x + 20f);
        cache.acceptAllRect = cache.fetchAllRect.RightOf(cache.fetchAllRect).WithWidth(labelStyle.CalcSize(AcceptAll).x + 20f);
        cache.differenceRect = cache.acceptAllRect.RightOf(cache.acceptAllRect).WithWidth(labelStyle.CalcSize(KeepUnity).x);
        cache.pushRect = cache.differenceRect.RightOf(cache.differenceRect).WithWidth(labelStyle.CalcSize(PushChanges).x);


		//EditorGUI.DrawRect(assetHeaderRect, Color.gray);
        if ( GUI.Button(cache.assetHeaderRect, "Asset Name (Click to Refresh)") ) {
			OnEnable();
		}
        if (GUI.Button(cache.uploadAllNewRect, UploadAllNew))
        {
            for (int i = 0; i < LocalizationRecords.Count; ++i)
            {
                var record = LocalizationRecords[i];
                var recommendation = record.GetSuggestedAction();
                if (recommendation == LocalizationSuggestedAction.UploadNewRecord)
                {
                    LocalizationRecordHelper.PushAssetRecord(record);
                }
            }
        }
        if (GUI.Button(cache.fetchAllRect, FetchAll))
        {
			FetchAllPages();
		}
        if ( GUI.Button(cache.acceptAllRect, AcceptAll) ) {
			for ( int i = 0; i < LocalizationRecords.Count; ++i ) {
				var record = LocalizationRecords[ i ];

				var recommendation = record.GetSuggestedAction();
				if ( recommendation == LocalizationSuggestedAction.AcceptChanges ) {
					record.AcceptRemoteChanges();
				}
			}
		}

		GUI.Button(cache.differenceRect, "Keep Unity");
		GUI.Button(cache.pushRect, "Push to Google");

		Color defaultBackground = GUI.backgroundColor;

        var nextRow = cache.toolbar.BelowSelf();

        var sheets = SheetCache?.Sheets;
		int rowCount = LocalizationRecords.Count + sheets.Length;

		var rect = nextRow.WithHeight(position.height-nextRow.y);
		var drawRect = new Rect(0f, 0f, position.width, rowCount * nextRow.height).SquashRight(20f);

		m_scroll = GUI.BeginScrollView(rect, m_scroll, drawRect);

		if (sheets != null) {
			for (int s = 0; s < sheets.Length; ++s) {
				GUI.backgroundColor = defaultBackground;

				var sheetName = sheets[s];

				if (m_scroll.y <= nextRow.y && nextRow.y < m_scroll.y + rect.height) {
					GUI.enabled = false;
					GUI.Button(nextRow, sheetName);
					GUI.enabled = true;
				}
				nextRow = nextRow.BelowSelf();

				for (int i = 0; i < LocalizationRecords.Count; ++i) {
					GUI.backgroundColor = defaultBackground;
					var record = LocalizationRecords[i];
					if (record.SheetName != sheetName) continue;

					if (m_scroll.y <= nextRow.y && nextRow.y < m_scroll.y + rect.height) {
						DrawRow(cache, ref nextRow, record);
					}
					nextRow = nextRow.BelowSelf();
				}
			}
			bool found = false;

			for (int i = 0; i < LocalizationRecords.Count; ++i) {
				GUI.backgroundColor = defaultBackground;

				var record = LocalizationRecords[i];
				if (sheets.Contains(record.SheetName) == false) {
					if (found == false) {
						GUI.enabled = false;
						GUI.Button(nextRow, "Unassigned");
						GUI.enabled = true;
						found = true;
						nextRow = nextRow.BelowSelf();
					}

					DrawRow(cache, ref nextRow, record);
					nextRow = nextRow.BelowSelf();
				}
			}

		}
		else {
			for (int i = 0; i < LocalizationRecords.Count; ++i) {
				GUI.backgroundColor = defaultBackground;
				var record = LocalizationRecords[i];
				DrawRow(cache, ref nextRow, record);
				nextRow = nextRow.BelowSelf();
			}
		}

		GUI.EndScrollView();
	}

    public void DrawRow(RectCache cache, ref Rect nextRow, LocalizationRecord record)
    {
        var recordName = nextRow.WithWidth(cache.assetHeaderRect.width);
        var uploadButton = recordName.RightOf(recordName).WithWidth(cache.uploadAllNewRect.width);
        var fetchButton = uploadButton.RightOf(uploadButton).WithWidth(cache.fetchAllRect.width);
        var acceptButton = fetchButton.RightOf(fetchButton).WithWidth(cache.acceptAllRect.width);
        var rejectButton = acceptButton.RightOf(acceptButton).WithWidth(cache.differenceRect.width);
        var pushButton = rejectButton.RightOf(rejectButton).WithWidth(cache.pushRect.width);

		var recommendation = record.GetSuggestedAction();

        switch (recommendation)
        {
            case LocalizationSuggestedAction.UploadNewRecord:
				case LocalizationSuggestedAction.AcceptChanges:
					GUI.backgroundColor = Color.green;
					break;
				case LocalizationSuggestedAction.ResolveConflicts:
					GUI.backgroundColor = Color.red;
					break;

				case LocalizationSuggestedAction.PushToRemote:
					GUI.backgroundColor = Color.yellow;
					break;
				case LocalizationSuggestedAction.FetchRemote:
					GUI.backgroundColor = Color.yellow;
					break;
            case LocalizationSuggestedAction.NoActionRequired:
                GUI.backgroundColor = Color.white;
                break;
			}

        if (GUI.Button(recordName, record.name))
        {
				Selection.activeInstanceID = record.GetInstanceID();
			}
        if (recommendation == LocalizationSuggestedAction.UploadNewRecord)
        {
            if (GUI.Button(uploadButton, "Upload New"))
            {
				LocalizationRecordHelper.PushAssetRecord(record);
            }
        }
        if (recommendation == LocalizationSuggestedAction.AcceptChanges)
        {
            if (GUI.Button(acceptButton.BelowSelf().BelowSelf(), "Use Google"))
            {
					record.AcceptRemoteChanges();
				}
            GUI.backgroundColor = Color.red;
            if (GUI.Button(rejectButton.BelowSelf(), "Use Unity"))
            {
                record.RejectRemoteChanges();
			}
				}
        if (recommendation == LocalizationSuggestedAction.ResolveConflicts)
        {
            if (GUI.Button(rejectButton.BelowSelf(), "Use Unity"))
            {
                record.RejectRemoteChanges();
			}
            GUI.backgroundColor = Color.red;
            if (GUI.Button(acceptButton.BelowSelf().BelowSelf(), "Use Google"))
            {
                record.AcceptRemoteChanges();
            }
				}
        if (recommendation == LocalizationSuggestedAction.PushToRemote)
        {
            if (GUI.Button(pushButton, "Push"))
            {
                LocalizationRecordHelper.PushAssetRecord(record);
			}
				}
        if (recommendation == LocalizationSuggestedAction.FetchRemote)
        {
            if (GUI.Button(fetchButton, "Fetch"))
            {
                LocalizationRecordHelper.FetchAssetRecord(record);
			}
        }

        if (recommendation == LocalizationSuggestedAction.AcceptChanges)
        {
            nextRow = nextRow.BelowSelf();
            GUI.Label(nextRow, record.Local.English);
            GUI.color = Color.green;
			nextRow = nextRow.BelowSelf();
            GUI.Label(nextRow, record.Remote.English);
            GUI.color = Color.white;
		}

        if (recommendation == LocalizationSuggestedAction.ResolveConflicts)
        {
            nextRow = nextRow.BelowSelf();
            GUI.Label(nextRow, record.Local.English);
            GUI.color = Color.red;
            nextRow = nextRow.BelowSelf();
            GUI.Label(nextRow, record.Remote.English);
            GUI.color = Color.white;
        }

        GUI.backgroundColor = Color.yellow;
	}

	[UsedImplicitly]
	public void Setup(IWindowsDockingService dockingService) {
		//m_selectedPage = 0;
		//m_model = model;
		//// Restore from last refresh
		//m_model.RPGSimulationPreset.Guid = guid;
		//m_views = views.ToArray();
		//m_viewCurrent = m_views[ m_selectedPage ];
		//UpdateModelInAllViews();
		m_isInitialized = true;

		titleContent = new GUIContent("Localization Viewer");
		/*
		if ( dockingService != null ) {
			var inspector = Nexus.Nexus.Instance.Container.Resolve<AnimationInspectorWindow>();
			dockingService.Dock( this, inspector, DockType.Right );
		}
		*/
	}

	[MenuItem( "Window/Localization Manager" )]
	public static void OpenWindow() {
		var ins = GetWindow<LocalizationRecordOverviewEditorWindow>("LocalizationRecord Manager", true, new Type[] { typeof(EditorGUI).Assembly.GetType("UnityEditor.InspectorWindow") });
		ins.minSize = new Vector2(840, 200);

		ins.Setup(null);

	}
}
