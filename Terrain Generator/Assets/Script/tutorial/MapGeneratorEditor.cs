using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator)target;
        //base.OnInspectorGUI();
        if (DrawDefaultInspector())
        {
            if (mapGenerator.autoUpdate)
            {
                mapGenerator.generateMap();
            }
        }
        if (GUILayout.Button("create"))
        {
            mapGenerator.generateMap();
        }
    }

}
