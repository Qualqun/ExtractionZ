using UnityEngine;
using UnityEngine.UI;

public class SoundSlider : MonoBehaviour
{
    Slider slider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider = GetComponent<Slider>();

        slider.value = 0.5f;
        slider.onValueChanged.AddListener(ChangeVolume);
    }


    void ChangeVolume(float volume)
    {
#if !UNITY_SERVER
        AkSoundEngine.SetRTPCValue("MasterVolume", volume * 100);
#endif

    }

}
