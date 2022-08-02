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
    TextMeshProUGUI nameTagDisplay;
    [SerializeField]
    Button mainToChangeNameButton;
    [Header("Change Name Menu")]
    [SerializeField]
    GameObject changeNamePanel;
    [SerializeField]
    TextMeshProUGUI changeNameWarningText;
    [SerializeField]
    TMP_InputField changeNameInputField;

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

    public void OpenMainMenu()
    {
        //Close All Windows
        changeNamePanel.SetActive(false);

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
}
