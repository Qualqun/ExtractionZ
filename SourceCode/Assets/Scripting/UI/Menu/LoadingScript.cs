#if !UNITY_SERVER
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class LoadingScript : MonoBehaviour
{


//    public ScenesGroup[] scenesGroup;

//#if UNITY_EDITOR
//    public ScenesGroupAsset[] allSceneAsset;
//#endif


    Transform[] childsLoading;

    Scene currentScene;

    bool onLoading = false;
    bool onUnloading = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        //SceneManager.UnloadSceneAsync("Loading");

        childsLoading = GetComponentsInChildren<Transform>();

        SetStateChilds(false);

        //Load menu scene at the start game
        LoadScene("MainMenus");
    }

    void SetStateChilds(bool state)
    {
        foreach (Transform children in childsLoading)
        {
            if (children != transform)
            {
                children.gameObject.SetActive(state);
            }
        }
    }


    public void LoadScene(string sceneName)
    {
        if (!onLoading)
        {
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            onLoading = true;
            async.completed += operation => onLoading = false; 
            
            if (currentScene.buildIndex >= 0)
            {
                var syncUnload = SceneManager.UnloadSceneAsync(currentScene);

                onUnloading = true;
                syncUnload.completed += operation => onUnloading = false;
            }

            currentScene = SceneManager.GetSceneByName(sceneName);
           
        }
    }

    private void Update()
    {
        SetStateChilds((onLoading || onUnloading));
    }

    //public IEnumerator LoadSceneCoroutine(string nameGroupToLoad)
    //{
    //    AsyncOperation[] allAsyncLoad = new AsyncOperation[0];
    //    string[] allSceneAsset = new string[0];
    //    bool allSceneAreLoaded = false;
    //    bool allSceneAreUnloaded = false;

    //    foreach (ScenesGroup group in scenesGroup)
    //    {
    //        if (group.nameGroup == nameGroupToLoad)
    //        {
    //            allSceneAsset = group.allSceneToLoad;
    //            allAsyncLoad = new AsyncOperation[allSceneAsset.Length];

    //        }
    //    }

    //    for (int i = 0; i < allSceneAsset.Length; i++)
    //    {
    //        allAsyncLoad[i] = SceneManager.LoadSceneAsync(allSceneAsset[i], LoadSceneMode.Additive);
    //        allAsyncLoad[i].allowSceneActivation = false;
    //    }

    //    SetStateChilds(true);

    //    //Load all scene inclued on in scenegroup
    //    while (!allSceneAreLoaded)
    //    {
    //        bool isLoaded = true;

    //        foreach (AsyncOperation asyncLoad in allAsyncLoad)
    //        {
    //            if (asyncLoad.progress < 0.9f)
    //            {
    //                isLoaded = false;
    //            }

    //            //Debug.Log(asyncLoad.ToString() + " " + asyncLoad.progress);
    //        }

    //        allSceneAreLoaded = isLoaded;

    //        yield return null;
    //    }

    //    foreach (AsyncOperation asyncLoad in allAsyncLoad)
    //    {
    //        asyncLoad.allowSceneActivation = true;
    //    }

    //    // Unload all scene not inclued in scenegroup
    //    while (!allSceneAreUnloaded)
    //    {
    //        allSceneAreUnloaded = true;

    //        for (int i = 0; i < SceneManager.sceneCount; i++)
    //        {
    //            if (SceneManager.GetSceneAt(i).IsValid())
    //            {
    //                bool needDeleted = true;

    //                foreach (string sceneAsset in allSceneAsset)
    //                {
    //                    if (sceneAsset == SceneManager.GetSceneAt(i).name || SceneManager.GetSceneAt(i).name == "Persistent" || SceneManager.GetSceneAt(i).isSubScene)
    //                    {
    //                        needDeleted = false;
    //                    }
    //                }

    //                if (needDeleted)
    //                {
    //                    SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i).name);
    //                    allSceneAreUnloaded = false;
    //                }
    //            }
    //        }


    //        if (allSceneAreUnloaded)
    //        {
    //            SetStateChilds(false);
    //        }

    //        yield return null;
    //    }


    //    yield return null;

    //}
}
#endif