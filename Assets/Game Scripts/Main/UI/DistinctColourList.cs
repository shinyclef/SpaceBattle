using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class DistinctColourList
{
    private static List<Color> colors;

    static DistinctColourList()
    {
        var rgb = new float3[]
        {
            new float3(60, 180, 75), // green
            new float3(0, 130, 200), // blue
            new float3(245, 130, 48), // orange
            new float3(255, 225, 25), // yellow
            new float3(230, 25, 75), // red
            new float3(70, 240, 240), // cyan
            new float3(240, 50, 230), // magenta
            new float3(250, 190, 190), // pink
            new float3(0, 128, 128), // teal
            new float3(230, 190, 255), // lavender
            new float3(170, 110, 40), // brown
            new float3(255, 250, 200), // beige
            new float3(128, 0, 0), // maroon
            new float3(170, 255, 195), // mint
            new float3(0, 0, 128) // navy blue
        };

        colors = new List<Color>();
        for (int i = 0; i < rgb.Length; i++)
        {
            colors.Add(new Color(rgb[i].x / 255f, rgb[i].y / 255f, rgb[i].z / 255f));
        }
    }

    public static Color GetColour(int index)
    {
        index = index % colors.Count;
        return colors[index];
    }
}