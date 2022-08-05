using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    EventSystem eventSystem;
    [Header("Main Menu")]
    [SerializeField]
    GameObject mainMenuPanel;
    [SerializeField]
    TextMeshProUGUI nameTagDisplay;
    [SerializeField]
    Button mainToChangeNameButton;
    [SerializeField]
    Button mainToOptionsButton;
    [SerializeField]
    Button mainToCreditsButton;
    [Header("Change Name Menu")]
    [SerializeField]
    GameObject changeNamePanel;
    [SerializeField]
    TextMeshProUGUI changeNameWarningText;
    [SerializeField]
    TMP_InputField changeNameInputField;
    [Header("Options Menu")]
    [SerializeField]
    GameObject optionsPanel;
    [SerializeField]
    GameObject optionsSelectedOnOpen;
    [Header("Credits Menu")]
    [SerializeField]
    GameObject creditsPanel;
    [SerializeField]
    GameObject creditsSelectedOnOpen;

    // Start is called before the first frame update
    void Start()
    {
        OpenMainMenu();
    }

    private void OnEnable()
    {
        changeNameInputField.onSubmit.AddListener(SubmitChangeNametag);
    }

    private void OnDisable()
    {
        changeNameInputField.onSubmit.RemoveListener(SubmitChangeNametag);
    }

    public void CloseAllWindows()
    {
        mainMenuPanel.SetActive(false);
        changeNamePanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);

    }

    public void OpenMainMenu()
    {
        CloseAllWindows();
        mainMenuPanel.SetActive(true);

        nameTagDisplay.text = PlayerAuthenticationManager.Instance.GetPlayerName().ToString();
    }

    public void OpenNameTagChange()
    {
        changeNamePanel.SetActive(true);
        changeNameInputField.text = "";
        eventSystem.SetSelectedGameObject(changeNameInputField.gameObject);
    }

    public void SubmitChangeNametag(string input)
    {
        bool success = PlayerAuthenticationManager.Instance.AttemptSetPlayerName(input);

        if (success)
        {
            changeNameWarningText.color = Color.white;

            eventSystem.SetSelectedGameObject(mainToChangeNameButton.gameObject);
            OpenMainMenu();
        }
        else
        {
            changeNameWarningText.color = Color.red;
        }
    }

    public void AbandonChangeNametag()
    {
        changeNameWarningText.color = Color.white;
        OpenMainMenu();
    }

    public void PlayGameButton()
    {
        SceneManager.LoadScene(2);
    }

    public void OpenOptions()
    {
        CloseAllWindows();
        optionsPanel.SetActive(true);
        eventSystem.SetSelectedGameObject(optionsSelectedOnOpen);
    }

    public void CloseOptions()
    {
        OpenMainMenu();
        eventSystem.SetSelectedGameObject(mainToOptionsButton.gameObject);
    }

    public void OpenCredits()
    {
        CloseAllWindows();
        creditsPanel.SetActive(true);
        eventSystem.SetSelectedGameObject(creditsSelectedOnOpen);
    }

    public void CloseCredits()
    {
        OpenMainMenu();
        eventSystem.SetSelectedGameObject(mainToCreditsButton.gameObject);
    }
}
