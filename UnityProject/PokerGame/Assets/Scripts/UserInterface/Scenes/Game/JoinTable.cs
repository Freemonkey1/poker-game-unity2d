using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using PokerGameClasses;

using TMPro;
using System;
using System.Net.Sockets;

using pGrServer;

// Ekran do wyboru stolika do do��czenia, z list� istniej�cych na serwerze stolik�w
public class JoinTable : MonoBehaviour
{
    // Przyciski menu ekranu
    [SerializeField] private Button joinButton;
    [SerializeField] private Button backToMenuButton;

    // Przyciski obok stolik�w do wyboru
    [SerializeField] private Button table1Button;
    [SerializeField] private Button table2Button;
    [SerializeField] private Button table3Button;
    [SerializeField] private Button table4Button;

    // informacje o b��dach, komunikaty dla gracza
    public GameObject PopupWindow;

    // Numer przycisku obok stolika, kt�ry zosta� wybrany
    private int chosenTable;

    // Nazwy stolik�w obok przycisk�w do ich wybierania
    //TODO (cz. PGGP-34) zrobi� z tego kiedy� tablic� zamiast oddzielnych zmiennych
    [SerializeField] private TMP_Text Table1;
    [SerializeField] private TMP_Text Table2;
    [SerializeField] private TMP_Text Table3;
    [SerializeField] private TMP_Text Table4;

    // Informacje o wst�pnie zaznaczonym stoliku, wy�wietlane w lewym dolnym rogu ekranu
    [SerializeField] private TMP_Text InfoPlayersCount;
    [SerializeField] private TMP_Text InfoBotsCount;
    [SerializeField] private TMP_Text InfoMinChips;
    [SerializeField] private TMP_Text InfoMinXP;

    // Start is called before the first frame update
    void Start()
    {
        if (MyGameManager.Instance.GameTableList == null)
            return;

        // Domy�lnie nie wybrano stolika
        this.chosenTable = -1;

        // Je�li stolik�w jest wi�cej ni� tyle ile si� zmie�ci w naszym menu (obecnie 4 opcje),
        // wy�wietlamy tylko 4 pierwsze z listy
        // (TODO (cz. PGGP-34) mo�e warto zmieni�, �eby wy�wietla� 4 najnowsze, czyli 4 ostatnie?) 
        int tablesToShow = MyGameManager.Instance.GameTableList.Count;
        if (tablesToShow > 4)
            tablesToShow = 4;

        // Pokazywanie nazwy danego stoliku obok przycisku wyboru
        // (TODO (cz. PGGP-34) dlatego warto by zamieni� to na tablic� tekst�w o stolikach,
        // �eby nie by�o tylu if'�w na przypadki ile mamy dost�pnych stolik�w)
        if (tablesToShow >= 1)
            this.Table1.text = MyGameManager.Instance.GameTableList[0].Name;
        if(tablesToShow >= 2)
            this.Table2.text = MyGameManager.Instance.GameTableList[1].Name;
        if (tablesToShow >= 3)
            this.Table3.text = MyGameManager.Instance.GameTableList[2].Name;
        if (tablesToShow >= 4)
            this.Table4.text = MyGameManager.Instance.GameTableList[3].Name;
    }

    // Update is called once per frame
    // TODO (cz. PGGP-69) jak zrobimy wysy�anie zapytania o stoliki co ile� czasu automatycznie,
    // to powinni�my tu wrzuci� update'owanie nazw dost�pnych stolik�w na ekranie
    // TODO (cz. PGGP-69) update'owana wtedy te� powinna by� zmienna 'chosenTable', bo m�g� znikn�� z serwera wybrany stolik
    void Update()
    {
        
    }

    public void OnJoinButton()
    {
        // Je�li nie ma dost�pnych stolik�w, cofa do ekranu PlayMenu
        if(MyGameManager.Instance.GameTableList.Count == 0)
        {
            Debug.Log("There are no game tables to join. Create one first");
            if (PopupWindow)
            {
                ShowPopup("There are no game tables to join. Create one first");
            }
            SceneManager.LoadScene("PlayMenu");
            return;
        }

        // Info, �e nie wybrano �adnego stolika (nie cofa, zostajemy w tym ekranie)
        if(this.chosenTable == -1)
        {
            Debug.Log("You didn't choose any game table. Choose one to join it by clicking the tick near it. ");
            if (PopupWindow)
            {
                ShowPopup("You didn't choose any game table. Choose one to join it by clicking the tick near it. ");
            }
            return;
        }

        // Pobranie wybranego przez gracza stolika z listy w MyGameManager
        GameTableInfo gameTable = MyGameManager.Instance.GameTableList[this.chosenTable];
        PlayerState player = MyGameManager.Instance.MainPlayer;

        // Sprawdzenie, czy gracz spe�nia warunki do��czenia do stolika
        // TODO mo�na by to doda� do osobnej metody
        if(Int32.Parse(gameTable.minXp) > player.Xp)
        {
            this.ShowPopup("You can't join this table. You don't have enough XP");
            return;
        }
        else if (Int32.Parse(gameTable.minChips) > player.TokensCount)
        {
            this.ShowPopup("You can't join this table. You don't have enough chips");
            return;
        }
        else // ok
        {
            Debug.Log("Added player " + player.Nick + " to " + gameTable.Name);

            // zapytanie o dodanie do stolika na serwerze
            byte[] tosend = System.Text.Encoding.ASCII.GetBytes(MyGameManager.Instance.clientToken + ' ' + "1" + ' ' + MyGameManager.Instance.GameTableList[this.chosenTable].Name + ' ');
            NetworkStream ns = MyGameManager.Instance.mainServerConnection.stream;
            ns.Write(tosend, 0, tosend.Length);

            // TODO (cz. PGGP-66) doda� czekanie na odpowied� od serwera, czy zostali�my dodani
            // do wybranego stolika, zanim przejdziemy do sceny stolika
            SceneManager.LoadScene("Table");
        }   
    }

    // TODO je�li dodamy tak� metod� do PopupText, wyrzuci�
    void ShowPopup(string text)
    {
        var popup = Instantiate(PopupWindow, transform.position, Quaternion.identity, transform);
        popup.GetComponent<TextMeshProUGUI>().text = text;
    }

    public void OnBackToMenuButton()
    {
        SceneManager.LoadScene("PlayMenu");
    }

    // Update info o wybranym stoliku w lewym dolnym rogu ekranu
    private bool UpdateGameTableInfo(GameTableInfo gameTable)
    {
        this.InfoPlayersCount.text = gameTable.HumanCount;
        this.InfoBotsCount.text = gameTable.BotCount;
        this.InfoMinChips.text = gameTable.minChips;
        this.InfoMinXP.text = gameTable.minXp;

        return true;
    }

    // Zapisanie numeru wybranego stolika po klikni�ciu przycisku obok niego, ze sprawdzaniem,
    // czy numer wybranego stolika nie jest wi�kszy, ni� liczba dost�pnych stolik�w
    public void OnTable1Button()
    {
        if (MyGameManager.Instance.GameTableList.Count >= 1)
        {
            this.chosenTable = 0;
            this.UpdateGameTableInfo(MyGameManager.Instance.GameTableList[this.chosenTable]);
        }
    }

    public void OnTable2Button()
    {
        if (MyGameManager.Instance.GameTableList.Count >= 2)
        {
            this.chosenTable = 1;
            this.UpdateGameTableInfo(MyGameManager.Instance.GameTableList[this.chosenTable]);
        }
    }

    public void OnTable3Button()
    {
        if (MyGameManager.Instance.GameTableList.Count >= 3)
        {
            this.chosenTable = 2;
            this.UpdateGameTableInfo(MyGameManager.Instance.GameTableList[this.chosenTable]);
        }
    }

    public void OnTable4Button()
    {
        if (MyGameManager.Instance.GameTableList.Count >= 4)
        {
            this.chosenTable = 3;
            this.UpdateGameTableInfo(MyGameManager.Instance.GameTableList[this.chosenTable]);
        }
    }
}
