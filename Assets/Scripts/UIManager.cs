using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject startMenu;
    public InputField usernameField;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("instance already exists");
            Destroy(this);
        }
    }

    public void ServerConnect()
    {
        startMenu.SetActive(false);
        usernameField.interactable = false;
        SceneManager.LoadScene(1);
        Debug.Log("swapped scene");
        Client.instance.ConnectToServer();


    }

}
