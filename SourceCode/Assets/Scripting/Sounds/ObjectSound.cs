using UnityEngine;
public enum TypeSoundObject
{
    MEGAPHONE,
    OTHER,
    LENGHT
}

public class ObjectSound : MonoBehaviour
{
    public TypeSoundObject typeObject = TypeSoundObject.OTHER;
}
