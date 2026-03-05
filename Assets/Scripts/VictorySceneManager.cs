using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictorySceneManager : MonoBehaviour
{
    public Button continueButton;

    void Start()
    {
        continueButton.onClick.AddListener(onContinueButtonClicked);
    }
    void onContinueButtonClicked()
    {
        SceneManager.LoadSceneAsync("StartScene", LoadSceneMode.Single);
    }
}
