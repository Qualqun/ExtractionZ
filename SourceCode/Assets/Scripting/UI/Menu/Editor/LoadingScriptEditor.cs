
//using UnityEditor;
//using UnityEngine;

//#if !UNITY_SERVER
//[CustomEditor(typeof(LoadingScript))]
//public class LoadingEditor : Editor
//{
//    SerializedProperty scenesGroupProperty;
//    SerializedProperty propertyAllScenes;
//    private void OnEnable()
//    {
//        propertyAllScenes = serializedObject.FindProperty("allSceneAsset");
//        scenesGroupProperty = serializedObject.FindProperty("scenesGroup");
//    }
//#if !UNITY_SERVER
//    public override void OnInspectorGUI()
//    {

//        LoadingScript loadingScript = (LoadingScript)target;
//        ScenesGroupAsset[] tempSceneGroup;

//        if (loadingScript.allSceneAsset.Length == 0 )
//        {
//            loadingScript.allSceneAsset = new ScenesGroupAsset[1];
//        }

//        tempSceneGroup = (ScenesGroupAsset[])loadingScript.allSceneAsset.Clone();
//        serializedObject.Update();

//        EditorGUILayout.PropertyField(scenesGroupProperty);

//        EditorGUILayout.PropertyField(propertyAllScenes);

//        serializedObject.ApplyModifiedProperties();

//        //conversion SceneAsset into string
//        if (tempSceneGroup != loadingScript.allSceneAsset)
//        {
//            loadingScript.scenesGroup = new ScenesGroup[loadingScript.allSceneAsset.Length];

//            for(int i = 0; i < loadingScript.scenesGroup.Length; i++)
//            {
//                int sizeSceneToLoad = loadingScript.allSceneAsset[i].loadSceneAsset.Length;

//                loadingScript.scenesGroup[i].nameGroup = loadingScript.allSceneAsset[i].nameGroup;
//                loadingScript.scenesGroup[i].allSceneToLoad = new string[sizeSceneToLoad];

//                for (int j = 0; j < sizeSceneToLoad; j++)
//                {
//                    if(loadingScript.allSceneAsset[i].loadSceneAsset[j] != null)
//                    {
//                        loadingScript.scenesGroup[i].allSceneToLoad[j] = loadingScript.allSceneAsset[i].loadSceneAsset[j].name;
//                    }
//                }
//            }
            
//        }

//    }
//#endif
//}

//#endif