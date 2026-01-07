#if !UNITY_SERVER

using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(AIPedSpawn))]
public class AIPedSpawnEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AIPedSpawn spawnZombie = (AIPedSpawn)target;

        SerializedProperty isTuto = serializedObject.FindProperty("isTuto");
        SerializedProperty nbZombies = serializedObject.FindProperty("nbIAPed");
        SerializedProperty nbZombiesSpawned = serializedObject.FindProperty("nbIAPedSpawned");
        SerializedProperty spawnRate = serializedObject.FindProperty("spawnRate");
        SerializedProperty allZones = serializedObject.FindProperty("allSpawns");

        spawnZombie.UpdateInfoSpawn();

        serializedObject.Update();

        EditorGUILayout.LabelField("Tuto Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isTuto, new GUIContent("Is Tuto"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Zombies", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nbZombies, new GUIContent("Nb AI per session"));

        GUI.enabled = false;
        EditorGUILayout.PropertyField(nbZombiesSpawned, new GUIContent("Nb AI spawned"));
        GUI.enabled = true;

        for (int i = 0; i < allZones.arraySize; i++)
        {
            SerializedProperty element = spawnRate.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Spawn " + spawnZombie.allSpawns[i].gameObject.name, EditorStyles.boldLabel);

            EditorGUILayout.Slider(element, 0, 100, new GUIContent("Spawn rate"));

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
