using TMPro;
using UnityEngine;

public class ChangePseudo : MonoBehaviour
{

    TMP_InputField inputField;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        inputField.text = Game.Instance.pseudo;
        inputField.onValueChanged.AddListener(UpdatePseudo);
    }

    void UpdatePseudo(string pseudo)
    {
        Game.Instance.pseudo = pseudo;
    }

}
