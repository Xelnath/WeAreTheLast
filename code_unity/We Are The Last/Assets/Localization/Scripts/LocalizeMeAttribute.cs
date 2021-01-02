using System;
using UnityEngine;

/// <summary>
/// This attribute is used to mark an existing field to use to create a new loc record
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class LocalizeMeAttribute : Attribute
{
    public string Sheet { get; set; }
    public string Key { get; set; }
    public string LocalizationRecord { get; set; }    

    public LocalizeMeAttribute(string localizationRecordFieldName, string sheet = null, string key = null)
    {
        LocalizationRecord = localizationRecordFieldName;
        Key = key;
        Sheet = sheet;
    }
}
