#if !UNITY_SERVER
using System;
using UnityEditor;

[Serializable]
public struct ScenesGroup
{
    public string[] allSceneToLoad;
    public string nameGroup;
}

#if UNITY_EDITOR

[Serializable]
public struct ScenesGroupAsset
{
    public string nameGroup;
    public SceneAsset[] loadSceneAsset;
}
#endif

#endif