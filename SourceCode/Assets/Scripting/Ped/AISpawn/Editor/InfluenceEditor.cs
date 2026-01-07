#if !UNITY_SERVER
using UnityEditor;
using UnityEngine;


[CanEditMultipleObjects]
[CustomEditor(typeof(InfluenceZones))]
public class InfluenceEditor : Editor
{
    // Représente la propriété sérialisée du tableau allInfluence
    SerializedProperty allInfluence;
    float[] oldSpawnRates;

    // Appelé quand l'inspecteur est ouvert ou recompilé
    private void OnEnable()
    {
        // On récupère la propriété "allInfluence" du composant cible
        allInfluence = serializedObject.FindProperty("allInfluence");

        oldSpawnRates = new float[allInfluence.arraySize];

        for (int i = 0; i < allInfluence.arraySize; i++)
        {
            SerializedProperty element = allInfluence.GetArrayElementAtIndex(i);
            SerializedProperty spawnRate = element.FindPropertyRelative("spawnRate");

            oldSpawnRates[i] = spawnRate.floatValue;
        }

    }

    // Méthode principale pour dessiner l'inspecteur custom
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        float totalSpawn = 0f;
        float[] spawnRates = new float[allInfluence.arraySize];
        float[] sizeInfluences = new float[allInfluence.arraySize];

        EditorGUILayout.LabelField("Zones d'influence", EditorStyles.boldLabel);


        // Capture des valeurs
        for (int i = 0; i < allInfluence.arraySize; i++)
        {
            SerializedProperty element = allInfluence.GetArrayElementAtIndex(i);
            SerializedProperty spawnRate = element.FindPropertyRelative("spawnRate");
            SerializedProperty sizeInfluence = element.FindPropertyRelative("sizeInfluence");

            sizeInfluences[i] = sizeInfluence.floatValue;

            spawnRates[i] = float.IsNaN(spawnRate.floatValue) ? 0f : spawnRate.floatValue; ;
            totalSpawn += spawnRates[i];
        }

        // Limite la somme totale à 100%
        if (totalSpawn > 100f)
        {
            float overflow = totalSpawn - 100f;

            // On redistribue l'excès sur les autres zones
            for (int i = 0; i < spawnRates.Length; i++)
            {
                if (spawnRates[i] == oldSpawnRates[i])
                {
                    float percent = spawnRates[i] / (totalSpawn == 0 ? 1 : totalSpawn);

                    spawnRates[i] -= overflow * percent;
                    spawnRates[i] = Mathf.Clamp(spawnRates[i], 0f, 100f);
                }
            }
        }
        else
        {
            float underFlow = 100f - totalSpawn;

            for (int i = 0; i < spawnRates.Length; i++)
            {
                float percent = spawnRates[i] / (totalSpawn == 0 ? 1 : totalSpawn);

                spawnRates[i] += underFlow * percent;
                spawnRates[i] = Mathf.Clamp(spawnRates[i], 0f, 100f);
            }
        }

        //Fait en sorte de s'adapter à la taille de la zone precedente
        for (int i = 1; i < sizeInfluences.Length; i++)
        {
            if (sizeInfluences[i - 1] > sizeInfluences[i])
            {
                sizeInfluences[i] = sizeInfluences[i - 1];
            }
        }

        for (int i = 0; i < allInfluence.arraySize; i++)
        {
            SerializedProperty element = allInfluence.GetArrayElementAtIndex(i);
            SerializedProperty sizeInfluence = element.FindPropertyRelative("sizeInfluence");
            SerializedProperty spawnRate = element.FindPropertyRelative("spawnRate");
            SerializedProperty colorZone = element.FindPropertyRelative("colorZone");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Zone #{i + 1}", EditorStyles.boldLabel);

            // Affiche la taille de la zone
            // EditorGUILayout.PropertyField(sizeInfluence, new GUIContent("Taille"));
            sizeInfluences[i] = EditorGUILayout.FloatField("Taille", sizeInfluences[i]);
            // Applique la nouvelle valeur de spawnRate avec un slider
            spawnRates[i] = EditorGUILayout.Slider("Taux de spawn", spawnRates[i], 0, 100);

            // Affiche la couleur de la zone
            EditorGUILayout.PropertyField(colorZone, new GUIContent("Couleur"));

            EditorGUILayout.EndVertical();

            // Réapplique la valeur dans la propriété
            spawnRate.floatValue = spawnRates[i];
            sizeInfluence.floatValue = sizeInfluences[i];

        }

        oldSpawnRates = spawnRates;
        serializedObject.ApplyModifiedProperties();

    }
}

#endif