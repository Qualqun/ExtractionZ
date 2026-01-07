using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialStep : MonoBehaviour
{
    [SerializeField]
    TutorialUi tutorialUi;

    Vector3[] positionsSteps;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        // -1 bcs get component take parent transform also 
        positionsSteps = new Vector3[allTransforms.Length - 1];

        for (int i = 1; i < allTransforms.Length; i++)
        {
            positionsSteps[i - 1] = allTransforms[i].position;
        }
    }

    // Update is called once per frame
    void Update()
    {

        #if !UNITY_SERVER
        if (Game.Instance.playerList.Count > 0)
        {
            Transform childTransform = Game.Instance.playerList[0].transform.GetChild(0);
            if (childTransform.position.z != 0)
            {
                for (int i = 0; i < positionsSteps.Length; i++)
                {
                    // child player 

                    Debug.Log("Difference player z : " + childTransform.position.z + " step " + positionsSteps[i].z);

                    if (tutorialUi.tutorialStep <= i && childTransform.position.z < positionsSteps[i].z)
                    {
                        tutorialUi.tutorialStep = i + 1;
                    }
                }
            }
        }
#endif
    }

    private void OnDestroy()
    {
        string[] targets = { "mixamorig:LeftHand_Rig", "mixamorig:RightHand_Rig" };

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    foreach (string target in targets)
                    {
                        if (child != null && child.name.Contains(target))
                        {
                            Debug.Log($"[OnDestroy] Deleting: {child.name}");
                            DestroyImmediate(child.gameObject);
                            break;
                        }
                    }
                }
            }
        }
    }
}
