using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroductionSceneManager : MonoBehaviour
{
    public Button continueButton;

    void Start()
    {
        continueButton.onClick.AddListener(onContinueButtonClicked);
    }
    void onContinueButtonClicked()
    {
        SceneManager.LoadSceneAsync("Teach1", LoadSceneMode.Single);
    }
}
