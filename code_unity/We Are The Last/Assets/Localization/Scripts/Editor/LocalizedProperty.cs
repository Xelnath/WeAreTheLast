using UnityEditor;

public struct LocalizedProperty
{
    public SerializedProperty prop;
    public LocalizedAttribute localizedData;
}

public struct LocalizeMeStringProperty
{
    public SerializedProperty prop;
    public LocalizeMeAttribute localizeMeData;
}