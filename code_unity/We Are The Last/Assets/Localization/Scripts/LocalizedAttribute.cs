using System;
using UnityEngine;
/// <summary>
/// This attribute is used to provide hints to the google sheets localizer
/// by providing a unique ID per field, allowing the editor to generate
/// string database IDs for Alex O'Smith's localization records easily.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class LocalizedAttribute : Attribute
{
	public string Sheet { get; set; }
	public string Key { get; set; }
	public int Lines { get; set; }

	public LocalizedAttribute(string key, string sheet = null, int lines = 1) {
		Key = key;
		Lines = Mathf.Max(lines, 1);
		Sheet = sheet;
	}
}

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class LocalizeFromAttribute : Attribute
{
	public string Field { get; set; }

	public LocalizeFromAttribute(string field) {
		Field = field;
	}
}