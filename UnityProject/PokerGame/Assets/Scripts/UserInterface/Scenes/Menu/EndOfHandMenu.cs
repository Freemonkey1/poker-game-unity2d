using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Menu wyboru co gracz chce zrobi� po zako�czeniu si� pojedynczego rozdania
// (na razie nigdy do niego nie docieramy) - TODO (cz. PGGP-64) zmieni� to XD
public class EndOfHandMenu : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitToMenuButton;
    [SerializeField] private Button kickBotButton;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnContinueButton()
    {
        SceneManager.LoadScene("Table");
    }
    public void OnExitToMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // TODO (cz. PGGP-55) jak ju� b�d� boty, to doda� t� funkcjonalno��
    public void OnKickBotButton()
    {
        SceneManager.LoadScene("Table");
    }
}

