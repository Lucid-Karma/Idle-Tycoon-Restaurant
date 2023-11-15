using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tool
{
    
    [CustomEditor(typeof(Transform), true)]
    [CanEditMultipleObjects]
    public class TransformEditor : Editor 
    {
        Editor defaultEditor;
        Transform transform;

        void OnEnable()
        {
            transform = target as Transform;
            defaultEditor = Editor.CreateEditor(targets, Type.GetType("UnityEditor.TransformInspector, UnityEditor"));
        }

        void OnDisable()
        {
            MethodInfo disableMethod = defaultEditor.GetType() //Type
                .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if(disableMethod != null)
                disableMethod.Invoke(defaultEditor, null);
            DestroyImmediate(defaultEditor);
        }

        public override void OnInspectorGUI() 
        {
            defaultEditor.OnInspectorGUI();
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();

            if(GUILayout.Button("Copy"))
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(transform);
            }

            if(GUILayout.Button("Paste"))
            {
                UnityEditorInternal.ComponentUtility.PasteComponentValues(transform);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}

