using Unity.Entities.UniversalDelegates;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayEvironementSounds))]
public class PlayEvironementSoundsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        PlayEvironementSounds script = (PlayEvironementSounds)target;


        EditorGUILayout.Space();

        GUI.enabled = false;

        for (int i = 0; i < (int)TypeSoundObject.LENGHT; i++)
        {
            EditorGUILayout.IntField("Nb " + ((TypeSoundObject)i).ToString() + " object", script.allObjectSound[i] == null ? 0 : script.allObjectSound[i].Count); 
        }

    }
}