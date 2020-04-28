using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//**to allow the classes that inherited this class to work, set the customEditor to true
[CustomEditor(typeof(UpdatableData),true)]
public class UpdatableDataEditor : Editor
{
#if UNITY_EDITOR
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UpdatableData data = (UpdatableData)target;

        if (GUILayout.Button("create"))
        {
            data.NotifiedOfUpdateValue();
            EditorUtility.SetDirty(target);
        }
    }
#endif
}
