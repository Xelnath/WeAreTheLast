using Quantum;
using Sini.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

//[CustomPropertyDrawer(typeof(String))]
public class LocalizeMeStringDrawer : PropertyDrawer
{
    //static GUIStyle _overlay;

    //static public GUIStyle OverlayStyle
    //{
    //    get {
    //        if (_overlay == null)
    //        {
    //            _overlay = new GUIStyle(EditorStyles.miniLabel);
    //            _overlay.alignment = TextAnchor.MiddleRight;
    //            _overlay.contentOffset = new Vector2(-24, 0);
    //            _overlay.normal.textColor = Color.red.Alpha(0.9f);
    //        }

    //        return _overlay;
    //    }
    //}

    public LocalizedProperty LocRecord;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var targetType = property.serializedObject.targetObject.GetType();
        var localizeMe = fieldInfo.GetCustomAttribute<LocalizeMeAttribute>();
        if ( localizeMe == null )
        {
            return 18f;
        }
        else
        {
            return 36f;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var targetType = property.serializedObject.targetObject.GetType();
        var localizeMe = targetType.GetField(property.name).GetCustomAttribute<LocalizeMeAttribute>();
        //propQuestTitle = new LocalizedProperty
        //{
        //    prop = serializedObject.FindProperty("QuestTitle"),
        //    localizedData = qdaType.GetField("QuestTitle").GetCustomAttribute<LocalizedAttribute>()
        //};
        if ( localizeMe == null )
        {
            Debug.Log("hi");
            EditorGUI.PropertyField(position, property, label);
        }
        else
        {
            SerializedProperty localRecordProp = property.serializedObject.FindProperty(localizeMe.LocalizationRecord);
            if ( localRecordProp == null)
            {
                EditorGUI.LabelField(position.TopHalf(), $"Missing LocalizationRecord: {localizeMe.LocalizationRecord}");
                EditorGUI.PropertyField(position.BottomHalf(), property, label);
            }
            else
            {
                var locRecord = localRecordProp.objectReferenceValue as LocalizationRecord;
                if (locRecord != null)
                {
                    EditorGUI.LabelField(position.TopHalf(), "Loc Record Found!");
                    EditorGUI.PropertyField(position.BottomHalf(), property, label);
                }
                else
                {
                    EditorGUI.LabelField(position.TopHalf(), "Create LocRecord");
                    EditorGUI.PropertyField(position.BottomHalf(), property, label);
                }
            }
        }
    }
}