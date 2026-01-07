
using System.Collections.Generic;
using UnityEngine;

public class PlayEvironementSounds : MonoBehaviour
{
    public List<GameObject>[] allObjectSound =  new List<GameObject>[(int)TypeSoundObject.LENGHT];
    public int[] countObject = new int[(int)TypeSoundObject.LENGHT];

    //public GameObject[] allObjectSound = new GameObject[(int)TypeObject.LENGHT];


    private void Start()
    {

        // Initialiser chaque liste dans allObjectSound
        for (int i = 0; i < (int)TypeSoundObject.LENGHT; i++)
        {
            if (allObjectSound[i] == null)
            {
                allObjectSound[i] = new List<GameObject>();
            }
        }


        foreach (ObjectSound objectSound in  FindObjectsByType<ObjectSound>(FindObjectsSortMode.None))
        {
            allObjectSound[(int)objectSound.typeObject].Add(objectSound.gameObject);
        }

        for (int i = 0; i < (int)TypeSoundObject.LENGHT; i++)
        {
            countObject[i] = allObjectSound[i].Count;
        }
            
    }

    public void PlaySound(int type, int objectNumber, uint soundId)
    {
        if (objectNumber < allObjectSound[type].Count)
        {
#if !UNITY_SERVER
            AkSoundEngine.PostEvent(soundId, allObjectSound[type][objectNumber]);
#endif
        }
        else
        {
            Debug.LogError(this.ToString() + " Hors range");
        }
    }
}
