using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugColors
{

    public static Color WalkColor = Quantum.PolymerColor.paper_brown_700.ToColor();
    public static Color WallColor = Quantum.PolymerColor.paper_amber_500.ToColor();
    public static Color CeilingColor = Quantum.PolymerColor.paper_purple_700.ToColor();

    public static Color FlightColor = Quantum.PolymerColor.paper_light_blue_300.ToColor();

	public static Color32 ToColor32(this ColorRGBA clr) {
		return new Color32(clr.R, clr.G, clr.B, clr.A);
	}

	public static Color ToColor(this ColorRGBA clr) {
		return new Color(clr.R / 255f, clr.G / 255f, clr.B / 255f, clr.A / 255f);
	}
}
