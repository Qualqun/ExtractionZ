#if !UNITY_SERVER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Pour utiliser le composant Text
using TMPro; // Pour utiliser TextMeshPro
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.Entities;

public enum UIElementEnum
{
    CROSS_HAIR = 1,
    WEAPON_CAPACITY_TEXT = 2,
    HEALTH_POINT_TEXT = 3,
    HIT_MARKER = 4
}
public class UIManager : MonoBehaviour
{
    private EntityManager entityManager;
    public static UIManager Instance { get; private set; }

    [SerializeField]
    private Dictionary<UIElementEnum, GameObject> uiElements = new Dictionary<UIElementEnum, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Garde l'instance à travers les scènes
        }
        else
        {
            Destroy(gameObject); // Détruit les doublons
            return; // Arrête l'initialisation pour éviter des erreurs
        }
    }

    private void Start()
    {
    
    }

    public void AddElement(UIElementEnum elementID, GameObject element)
    {
        if (!uiElements.ContainsKey(elementID))
        {
            uiElements.Add(elementID, element);
         //   Debug.Log($"[UIManager::AddElement] - Élément ajouté : {elementID}");
        }
        else
        {
            //Debug.LogWarning($"[UIManager::AddElement] - L'élément {elementID} existe déjà dans le dictionnaire.");
        }
    }

    public void RemoveElement(UIElementEnum elementID)
    {
        if (uiElements.ContainsKey(elementID))
        {
            uiElements.Remove(elementID);
            //Debug.Log($"[UIManager::RemoveElement] - Élément supprimé : {elementID}");
        }
        else
        {
            //Debug.LogWarning($"[UIManager::RemoveElement] - L'élément {elementID} n'existe pas dans le dictionnaire.");
        }
    }

    public void SetActiveElement(UIElementEnum elementID, bool isActive)
    {
        GameObject element = GetElement(elementID);

        if (element != null)
        {
            element.SetActive(isActive);
            //Debug.Log($"[UIManager::SetActiveElement] - {elementID} est maintenant {(isActive ? "actif" : "inactif")}");
        }
        else
        {
            //Debug.LogWarning($"[UIManager::SetActiveElement] - Impossible de définir l'état de {elementID}, élément non trouvé.");
        }
    }

    public GameObject GetElement(UIElementEnum elementID)
    {
        if (uiElements.TryGetValue(elementID, out GameObject element))
        {
            return element;
        }
        else
        {
            Debug.LogWarning($"[UIManager::GetElement] - L'élément {elementID} n'existe pas dans le dictionnaire.");
            return null;
        }
    }

    public void SetElementText(UIElementEnum elementID, string newText, Color? newColor = null)
    {
        GameObject element = GetElement(elementID);

        if (element != null)
        {
            TextMeshProUGUI tmpText = element.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = newText;

                if (newColor.HasValue)
                {
                    tmpText.color = newColor.Value;
                }

              //  Debug.Log($"[UIManager::SetElementText] - Texte de {elementID} mis à jour à: {newText} (TextMeshPro)");
            }
            else
            {
                Debug.LogWarning($"[UIManager::SetElementText] - Aucun composant TextMeshPro trouvé sur {elementID}");
            }
        }
        else
        {
            Debug.LogWarning($"[UIManager::SetElementText] - Impossible de définir le texte de {elementID}, élément non trouvé.");
        }
    }


    #region DropDownEvents

    public void OnSetFPSDropDownValueChanged(TMP_Dropdown dropDown)
    {
        // Vérifie la valeur sélectionnée
        switch (dropDown.value)
        {
            case 0: // 30 FPS
                Application.targetFrameRate = 30;
                QualitySettings.vSyncCount = 1; // Active la synchronisation verticale
                break;

            case 1: // 60 FPS
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 1; // Active la synchronisation verticale
                break;

            case 2: // 144 FPS
                Application.targetFrameRate = 144;
                QualitySettings.vSyncCount = 0; // Désactive la synchronisation verticale
                break;

            default:
                //Debug.LogWarning("[UIManager::OnSetFPSDropDownValueChanged] - FPS value not recognized.");
                break;
        }

        //Debug.Log($"[UIManager::OnSetFPSDropDownValueChanged] - FPS set to: {Application.targetFrameRate}");
    }
    #endregion

    #region ToggleEvents

    public void OnFullScreenToggleChanged(Toggle toggle)
    {
        if (toggle.isOn)
        {
            Screen.fullScreen = true;
            //Debug.Log("[UIManager::OnFullScreenToggleChanged] - Mode plein écran activé.");
        }
        else
        {
            Screen.fullScreen = false;
            //Debug.Log("[UIManager::OnFullScreenToggleChanged] - Mode plein écran désactivé.");
        }
    }


    #endregion
}
#endif