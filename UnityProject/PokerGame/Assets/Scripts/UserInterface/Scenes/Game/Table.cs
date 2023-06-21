using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using PokerGameClasses;
using pGrServer;
using System.Threading;

using TMPro;
using System;
using System.Net.Sockets;


using static System.Net.Mime.MediaTypeNames;

// G��wny ekran gry - widok stolika, kart i graczy
public class Table : MonoBehaviour
{
    // Przyciski ruch�w gracza (menu w lewym dolnym rogu ekranu)
    [SerializeField] private Button checkButton;
    [SerializeField] private Button allInButton;
    [SerializeField] private Button passButton;
    [SerializeField] private Button bidButton;

    // Dane pobrane z pola input 'Bid' (menu w lewym dolnym rogu ekranu)
    private string betFieldText;

    // Wy�wietlanie informacji o g��wnym graczu
    /*
     * - Nick
     * - Ile ma �eton�w
     * - Ile postawi� �eton�w w tym rozdaniu
     * - Jego ikona (GameObject)
     * - Jego karty (GameObject'y)
     */
    [SerializeField] private TMP_Text InfoMainPlayerName;
    [SerializeField] private TMP_Text InfoMainPlayerChips;
    [SerializeField] private TMP_Text InfoMainPlayerBid;
    [SerializeField] private GameObject InfoMainPlayerIcon;
    [SerializeField] private GameObject[] MainPlayerCards;


    // Obiekt menu ruch�w gracza, u�ywamy go do chowania lub pokazywania tego menu
    [SerializeField]
    private CanvasRenderer menuCanvas;

    // Lista sprite'�w kart, z kt�rej wybieramy odpowiedni
    // sprite do przypisania do GameObject'u karty gracza
    [SerializeField]
    private CardsSprites collection;

    // Lista sprite'�w �eton�w, z kt�rej wybieramy odpowiedni
    // sprite, �eby zwizualizowa� �etony
    [SerializeField]
    private ChipsSprites chipsSprites;

    // Prze��cznik ustawiany na 'true', kiedy serwer przy�le do klienta zapytanie o wykonanie ruchu
    private bool readyToSendMove = false;

    // Stan stolika, odebrany od serwera (wysy�a po ka�dym ruchu kogokolwiek)
    private GameTableState gameTableState;
    // Stany wszystkich graczy, odebrane od serwera (wysy�a po ka�dym ruchu kogokolwiek).
    // S�ownik: klucz - Nick danego gracza (odczytywany z PlayerState), warto�� - PlayerState
    private IDictionary<string, PlayerState> playersStates;

    // informacje o b��dach, komunikaty dla gracza
    // m.in. komunikat o tym czyj ruch jest teraz
    public GameObject PopupWindow;

    // GameObject'y na ekranie (update'ujemy je, �eby wy�wietla�y info z gry na ekranie)
    // GameObject'y kart stolika (TYLKO te 5 kart, kt�re le�� na stoliku!)
    private GameObject[] CardsObject;
    // GameObject'y graczy (w nich s� te� GameObject'y ich kart)
    private GameObject[] Players
    {get; set;}

    // Prze��czniki, kt�re wykorzystujemy w Update'owaniu sceny, �eby wiedzie�,
    // kiedy mamy wy�wietli� dany tekst informacyjny
    bool displayPlayerTurnPopup = false;
    bool displayWinnerPopup = false;

    // Nick zwyci�zcy gry, od serwera (wysy�a pod koniec gry)
    string winnerNick = null;


    // Start is called before the first frame update
    void Start()
    {

        ShowMenu(false); //zakrycie MENU na start
        //ShowMenu(true);
        if (MyGameManager.Instance.MainPlayer == null)
            return;

        // Pierwszy update wy�wietlanego info g��wnego gracza
        this.InfoMainPlayerName.text = MyGameManager.Instance.MainPlayer.Nick;
        this.InfoMainPlayerChips.text = Convert.ToString(MyGameManager.Instance.MainPlayer.TokensCount) + " $";
        this.InfoMainPlayerBid.text = "Bet\n" + Convert.ToString(0) + " $";

        //Pobranie GameObject'�w przygotowanych na graczy i na karty stolika ze sceny
        //(Graczy mamy na sztywno utworzonych na scenie, a nie spawn'owanych po doj�ciu kogo� do stolika,

        //wi�c tutaj pobieramy wszystkie te puste szablony przygotowane na wy�wietlanie informacji o danym graczu)
        this.Players = InitPlayers();
        this.CardsObject = GameObject.FindGameObjectsWithTag("Card");
        //TestHidingCards();

        //Inicjalizacja stanu stolika i s�ownika stan�w graczy w grze
        this.gameTableState = new GameTableState();
        this.playersStates = new Dictionary<string, PlayerState>();
        HideAllPlayers();

        // W��czenie osobnego w�tku do komunikacji z serwerem na porcie od komunikat�w z gry
        // W tym w�tku Unity nie pozwala zmienia� nic na ekranie - update'owa� wygl�d
        // ekranu mo�na tylko w w�tku g��wnym, w kt�rym dzia�a np. funkcja Start i Update
        new System.Threading.Thread(CommunicateWithServer).Start();
    }

    public void CommunicateWithServer()
    {
        // Pobranie strumienia do komunikacji z serwerem na porcie od zdarze� gry
        // (w��czamy te ��cze obecnie w ekranie Login po udanym logowaniu) - TODO mo�e przenie�� to dopiero do tego ekranu?
        NetworkStream gameStream = MyGameManager.Instance.gameServerConnection.stream;
        bool running = true;

        /* P�tla do odbierania komunikat�w od serwera
         * Tutaj odbierane s�:
         * - zapytania od serwera o wykonanie ruchu przez gracza
         * - komunikaty od serwera z informacjami o stanie stolika i wszystkich graczy (w tym ich kartach)
         * (TODO (PGGP-54) zmieni�, �eby odbierane by�y tylko karty naszego gracza, a reszty tylko na koniec gry - ale to akurat na serwerze)
         * - komunikaty od serwera czyj ruch i pod koniec gry, kto zwyci�y�
         */
        while (running)
        {
            if (gameStream.DataAvailable)
            {
                UnityEngine.Debug.Log("sa dane na strumieniu");
                string gameRequest = NetworkHelper.ReadNetworkStream(gameStream);
                gameStream.Flush();

                /* Wiadomo�ci o zdarzeniach z gry od serwera s� rozdzielane znacznikiem :G: (od Game)
                 * i maj� posta� Typ_wiadomo�ci|Tre��
                 * Podczas przesy�ania wiadomo�ci o stanie gry wysy�anych jest na raz kilka wiadomo�ci:
                 * (wszystkie zaczynaj� si� od znacznika :G:, wi�c w sumie to osobne wiadomo�ci, ale to tak dla jasno�ci jak to dzia�a)
                 * - wiadomo�� typu "Round" (kt�ra to runda gry) TODO (cz. PGGP-44) doda� jej odbieranie tu i wy�wietlanie gdzie� na g�rze ekranu numeru rundy
                 * - wiadomo�� typu "Table state"
                 * - kilka wiadomo�ci typu "Player state" (o stanie ka�dego z graczy)
                 * - wiadomo�� typu "Which player turn"
                 */
                // TODO (cz. PGGP-44) doda� jeszcze odbieranie wiadomo�ci typu 'Info' i wy�wietlanie takich komunikat�w na ekranie
                string[] splittedRequests = gameRequest.Split(new string(":G:"));

                foreach (string singleRequest in splittedRequests)
                {
                    Debug.Log(singleRequest);
                    string[] splitted = singleRequest.Split(new string("|"));
                    if (splitted[0] == "Which player turn") // komunikat czyj teraz ruch
                    {
                        if(!this.displayPlayerTurnPopup)
                        {
                            CommunicatePlayersTurn(splitted[1]);
                        }
                    }
                    else if (splitted[0] == "Move request") // zapytanie o wykonanie ruchu
                    {
                        MoveRequestResponse(splitted);
                    }
                    else if (splitted[0] == "Table state") // komunikat stanu stolika
                    {
                        this.gameTableState.UnpackGameState(splitted); // Tre�� wiadomo�ci o stanie stolika ma jak�� struktur�, kt�r� odpakowuje ta metoda
                        Debug.Log(this.gameTableState);
                    }
                    else if (splitted[0] == "Player state") // komunikat stanu kt�rego� z graczy
                    {
                        PlayerState playerState = new PlayerState();
                        playerState.UnpackGameState(splitted); // Tre�� wiadomo�ci o stanie gracza ma jak�� struktur�, kt�r� odpakowuje ta metoda
                        Debug.Log(playerState);
                        this.playersStates[playerState.Nick] = playerState;
                        Debug.Log("Player state count: " + this.playersStates.Count);
                    }
                    else if(splitted[0] == "Winner") // komunikat z Nick'iem zwyci�zcy
                    {
                        this.winnerNick = splitted[1];
                        this.displayWinnerPopup = true;
                    }
                }
            }
        }
    }

    // Przestawiene prze��cznika, �eby metoda Update w g��wnym w�tku wiedzia�a, �e ma pokaza� Popup
    // (inne w�tki ni� g��wny nie mog� zmienia� wygl�du sceny w Unity)
    void CommunicatePlayersTurn(string currentPlayer)
    {
        if (currentPlayer == MyGameManager.Instance.MainPlayer.Nick)
        {
            this.displayPlayerTurnPopup = true;
        }
    }

    // Kiedy przyjdzie zapytanie od serwera o ruch gracza, ustawiamy prze��cznik,
    // �e gracz mo�e teraz wys�a� jeden ruch (prze��cznik si� przestawia ponownie na 'false'
    // w trakcie wysy�ania ruchu przez gracza (po klikni�ciu przez niego kt�rego� z przycisk�w od ruch�w)
    void MoveRequestResponse(string[] splitted)
    {
        Debug.Log(splitted[0]);
        Debug.Log(splitted[1]);
        this.readyToSendMove = true;
        //Czekamy teraz na klikniecie ktoregos z przyciskow. wyslanie kolejnego requesta do serwera jest wykonywane w metodach przyciskow
    }

    void UpdateChipsBidInGame(int amount)
    {
        DeleteChipsBitInGame();
        ShowChipsBidInGame(amount);
    }
    //Usuwanie �eton�w ze �rodka 
    void DeleteChipsBitInGame()
    {
        GameObject chips = GameObject.FindGameObjectWithTag("Chips");
        GameObject chipsContainer;
        try
        {
            chipsContainer = chips.transform.Find("Chips").gameObject;
            Destroy(chipsContainer);
        }
        catch(Exception e) { }
        GameObject chipsText = chips.transform.Find("Bet/BetText").gameObject;
        chipsText.GetComponent<TMP_Text>().enabled = false;
    }
    //Utworzenie GameObject pojedynczego �etonu i przypisanie mu sprite'a oraz pozycji
    void CreateChip(GameObject chipsContainer, Vector3 position, Sprite chipSprite)
    {
        GameObject chipsCanvas = GameObject.FindGameObjectWithTag("Chips");
        GameObject chip = new("Chip");
        chip.transform.parent = chipsContainer.transform;
        UnityEngine.UI.Image imageOfChipComponent = chip.AddComponent<UnityEngine.UI.Image>();
        imageOfChipComponent.sprite = chipSprite;
        chip.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
        chip.transform.localPosition = position;
    }

    //Podzial liczby zetonow na kupki o odpowiednich wartosciach
    Tuple<int,int[]> DivisionIntoChips(int amount)
    {
        int tempAmount = amount;

        int[] chipsValue = { 1, 2, 5, 10, 20, 25, 50, 100, 250, 500, 1000 };
        int[] amountOfChipsInStack = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        if (amount < 600)
        {
            for (int i = 8; i >= 0; i--)
            {
                while (tempAmount >= chipsValue[i])
                {
                    tempAmount = tempAmount - chipsValue[i];
                    amountOfChipsInStack[i] = amountOfChipsInStack[i] + 1;
                }
            }
        }
        else if (amount < 1200)
        {
            for (int i = 9; i >= 0; i--)
            {
                while (tempAmount >= chipsValue[i])
                {
                    tempAmount = tempAmount - chipsValue[i];
                    amountOfChipsInStack[i] = amountOfChipsInStack[i] + 1;
                }
            }
        }
        else
        {
            for (int i = 10; i >= 0; i--)
            {
                while (tempAmount >= chipsValue[i])
                {
                    tempAmount = tempAmount - chipsValue[i];
                    amountOfChipsInStack[i] = amountOfChipsInStack[i] + 1;
                }
            }
        }
        int numberOfStacks = 0;
        for (int i = 0; i < amountOfChipsInStack.Length; i++)
        {
            if (amountOfChipsInStack[i] != 0)
                numberOfStacks += 1;
        }
        return Tuple.Create(numberOfStacks, amountOfChipsInStack);
    }

    //Wyswietlanie tekstowe liczby zetonow oraz zarzadzanie wyswietlaniem zetonow
    void ShowChipsBidInGame(int amount)
    {
        GameObject chips = GameObject.FindGameObjectWithTag("Chips");
        GameObject chipsText = chips.transform.Find("Bet/BetText").gameObject;
        GameObject bet = chips.transform.Find("Bet").gameObject;
        chipsText.GetComponent<TMP_Text>().enabled = true;
        chipsText.GetComponent<TMP_Text>().text = amount.ToString() +"$";
        (int numberOfStacks, int[]amountOfChipsInStack) = DivisionIntoChips(amount);

        float positionX = 0 - 40 * (numberOfStacks / 2) - 50;
        float positionY = 0;
        GameObject chipsContainer = new("Chips");
        chipsContainer.transform.parent = chips.transform;
        chipsContainer.transform.position = chips.transform.position;
        for (int i = 0; i < amountOfChipsInStack.Length; i++)
        {
            positionY = -6;
            if (amountOfChipsInStack[i] != 0)
            {
                positionX += 50;
                Sprite chipSprite = chipsSprites.chipsSpriteSerialization[i]; //wybor sprite'a
                for (int j = 0; j < amountOfChipsInStack[i]; j++)
                {
                    positionY += 5;
                    CreateChip(chipsContainer,new Vector3(positionX, positionY, 0.0f), chipSprite);
                }
            }
        }
    }
    // Testowanie pokazywania i ukrywania odpowiednich kart u graczy
    // oraz chowania i pokazywania tak�e graczy
    //
    public void TestHidingCards()
    {
        
        HideAllPlayers();
        ShowPlayerOnTable(0, "Player1");
        ChangePlayerBet(100, 0);
        ChangePlayerMoney(200, 0);
        HidePlayerOnTable(2);

        Card card1 = new Card(CardSign.Heart, CardValue.Jack, 9);
        Card card2 = new Card(CardSign.Diamond, CardValue.Ace, 38);
        Card card3 = new Card(CardSign.Club, CardValue.Eight, 45);
        Card card4 = new Card(CardSign.Heart, CardValue.Four, 2);
        Card card5 = new Card(CardSign.Heart, CardValue.Five, 3);


        ShowCardOnDeck(card1, 0);
        ShowCardOnDeck(card2, 1);
        ShowCardOnDeck(card3, 2);
        ShowCardOnDeck(card4, 3);
        ShowCardOnDeck(card5, 4);
        List<Card> c = new List<Card>();
        c.Add(card1);
        c.Add(card2);

        CardsCollection cc = new CardsCollection(c);
        ShowPlayerCards(0, cc);
        ShowMainPlayerCards(cc);
        HidePlayerCards(0);
        HideMainPlayerCards();
        HideCardsOnDeck();

        ShowPlayerOnTable(1, "lala");
        ShowPlayerOnTable(2, "baba");
        HidePlayerCards(1);
        GraphicPass(true, true);
        GraphicPass(true, false, 1) ;
        GraphicWaitingForGame(true, true);
        GraphicWaitingForGame(true, false, 1);

        ShowChipsBidInGame(585);
        DeleteChipsBitInGame();
        ShowChipsBidInGame(1203);
    }
    int CompareObNames(GameObject x, GameObject y) { return x.name.CompareTo(y.name); }
    GameObject[] InitPlayers()
    {
        GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");
        Array.Sort(Players, CompareObNames);
        return Players;
    }

    // Metody od pokazywania i chowania kart, graczy i menu ruch�w
    // TODO przenie�� to do jakiej� osobnej klasy?

    //Funkcja pomocnicza dla GraphicPass
    void ChangingPlayerVisibility(bool makeInvisible, GameObject avatar, GameObject nick, GameObject bet)
    {
        if (makeInvisible == true) //Pasowanie
        {
            avatar.GetComponent<UnityEngine.UI.Image>().color = new Color32(255, 255, 255, 100);
            nick.GetComponent<TMP_Text>().color = new Color32(255, 255, 255, 100);
            bet.GetComponent<TMP_Text>().color = new Color32(255, 255, 255, 100);
        }
        else //Odpasowywanie
        {
            avatar.GetComponent<UnityEngine.UI.Image>().color = new Color32(255, 255, 255, 255);
            nick.GetComponent<TMP_Text>().color = new Color32(255, 255, 255, 255);
            bet.GetComponent<TMP_Text>().color = new Color32(255, 255, 255, 255);
        }
    }

    //Funkcja pokazujaca pasowanie lub cofajaca pasowanie, (ukrywanie lub pokazanie gracza, zostawienie kart)
    //Dla glownego gracza
    void GraphicPass(bool isPassing, bool isMainPlayerPassing)
    {
        GameObject avatar = InfoMainPlayerIcon;
        GameObject nick = InfoMainPlayerName.gameObject;
        GameObject bet = InfoMainPlayerBid.gameObject;
        ChangingPlayerVisibility(isPassing, avatar, nick, bet);
    }

    //Funkcja pokazujaca pasowanie lub cofajaca pasowanie, (ukrywanie lub pokazanie gracza, zostawienie kart)
    //Dla gracza o konkretnym numerze siedzenia
    void GraphicPass(bool isPassing, bool isMainPlayerPassing, int seatNumber) 
    {
        GameObject avatar = Players[seatNumber].transform.Find("Icon").gameObject;
        GameObject nick = Players[seatNumber].transform.Find("Informations/Name/NickText").gameObject;
        GameObject bet = Players[seatNumber].transform.Find("Informations/Bet/BetText").gameObject;
        ChangingPlayerVisibility(isPassing, avatar, nick, bet);
    }

    //Funkcja pomocnicza dla GraphicWaitingForGame
    void ChangingCardsVisibility(bool makeInvisible, GameObject card1, GameObject card2, GameObject bet)
    {
        if (makeInvisible == true) //Czekanie na gre - wylaczenie widocznosci kart
        {
            card1.GetComponent<UnityEngine.UI.Image>().enabled = false;
            card2.GetComponent<UnityEngine.UI.Image>().enabled = false;
            bet.GetComponent<TMP_Text>().enabled = false;
        }
        else //Odczekowywanie na gre - pokazanie widocznosci kart
        {
            card1.GetComponent<UnityEngine.UI.Image>().enabled = true;
            card2.GetComponent<UnityEngine.UI.Image>().enabled = true;
            bet.GetComponent<TMP_Text>().enabled = true;
        }
    }
    //Funcja pokazujaca lub cofajaca pokazywanie czekania gracza na kolejna gre
    //Dla gracza glownego
    void GraphicWaitingForGame(bool isWaiting, bool isMainPlayerWaiting)
    {
        if (isMainPlayerWaiting == true)
        {
            GameObject card1 = MainPlayerCards[0];
            GameObject card2 = MainPlayerCards[1];
            GameObject bet = InfoMainPlayerBid.gameObject;
            ChangingCardsVisibility(isWaiting, card1, card2, bet);
        }
    }
    //Funcja pokazujaca lub cofajaca pokazywanie czekania gracza na kolejna gre
    //Dla gracza o konkretnym numerze siedzenia
    void GraphicWaitingForGame(bool isWaiting, bool isMainPlayerWaiting, int seatNumber)
    {
        if (isMainPlayerWaiting == false)
        {
            GameObject card1 = Players[seatNumber].transform.Find("Cards/Card 1").gameObject;
            GameObject card2 = Players[seatNumber].transform.Find("Cards/Card 2").gameObject;
            GameObject bet = Players[seatNumber].transform.Find("Informations/Bet/BetText").gameObject;
            ChangingCardsVisibility(isWaiting, card1, card2, bet);
        } 
    }

    void ShowCard(Card card, GameObject cardObject)
    {
        cardObject.GetComponent<UnityEngine.UI.Image>().sprite = collection.cardsSpriteSerialization[card.Id];
    }

    void ShowPlayerCards(int seatNumber, CardsCollection cards)
    {
        if (cards == null)
        {
            Card cardBackSprite = new Card(0, 0, 52);
            for (int i = 0; i < MainPlayerCards.Length; i++)
                ShowCard(cardBackSprite, Players[seatNumber].transform.Find("Cards/Card " + (i + 1)).gameObject);
            return;
        }

        for (int i = 0; i < cards.Cards.Count; i++)
        {
            if (i >= 2)
                break;

            ShowCard(cards.Cards[i], Players[seatNumber].transform.Find("Cards/Card "+(i+1)).gameObject);
        }
    }
    void ShowMainPlayerCards(CardsCollection cards)
    {
        if (cards == null)
        {
            Card cardBackSprite = new Card(0, 0, 52);
            for (int i = 0; i < MainPlayerCards.Length; i++)
                ShowCard(cardBackSprite, MainPlayerCards[i]);
            return;
        }

        for (int i = 0; i < cards.Cards.Count; i++)
        {
            if (i >= 2)
                break;

            ShowCard(cards.Cards[i], MainPlayerCards[i]);
        }
    }

    void ShowCardOnDeck(Card card, int cardIdToShow)
    {
        ShowCard(card, CardsObject[cardIdToShow]);
    }
    void HideCard(GameObject cardObject) //Funkcja pomocnicza, ukrywa karte
    {
        cardObject.GetComponent<UnityEngine.UI.Image>().sprite = collection.cardsSpriteSerialization[52];
    }
    void HidePlayerCards(int seatNumber) //Ukrywanie kart gracza
    {
        HideCard(Players[seatNumber].transform.Find("Cards/Card 1").gameObject);
        HideCard(Players[seatNumber].transform.Find("Cards/Card 2").gameObject);
    }
    public void HidePlayerOnTable(int seatNumber) //Ukrywanie gracza i jego kart 
    {
        Players[seatNumber].transform.localScale = Vector3.zero;
    }
    void HideMainPlayerCards()//Ukrywanie kart glownego gracza (tego na srodku)
    {
        HideCard(MainPlayerCards[0]);
        HideCard(MainPlayerCards[1]);
    }
    void HideCardsOnDeck() //Karty na stole wylozone
    {
        for(int i = 0; i < CardsObject.Length; i++)
        {
            HideCard(CardsObject[i]);
        }
    }

    void HideAllPlayers() //Ukrywanie wszystkich graczy i ich kart
    { 
        foreach (GameObject player in Players)
        {
            player.transform.localScale = Vector3.zero;
        }

    }

    public void ShowPlayerOnTable(int seatNumber, string playerNick)
    {
        GameObject nick = Players[seatNumber].transform.Find("Informations/Name/NickText").gameObject;
        if (nick != null)
        {
            nick.GetComponent<TMP_Text>().text = playerNick;
            nick.GetComponent<TMP_Text>().fontSize = 21.75f;    //nie dziala, bo autosize w unity
        }
        Players[seatNumber].transform.localScale = Vector3.one;
    }

    public void ShowMenu(bool isMenuToShow)
    {
        if(isMenuToShow == true)
            menuCanvas.transform.localScale = Vector3.one;
        else
            menuCanvas.transform.localScale = Vector3.zero;
    }
    
    // Aktualizacja info danego gracza o jego zak�adzie i ile mu zosta�o �eton�w
    public void ChangePlayerBet(int amount, int seatNumber)
    {
        GameObject bet = Players[seatNumber].transform.Find("Informations/Bet/BetText").gameObject;
        if (bet != null)
        {
            bet.GetComponent<TMP_Text>().text = "Bet\n"+amount.ToString()+" $";
        }
    }
    public void ChangePlayerMoney(int amount, int seatNumber) 
    {
        GameObject money = Players[seatNumber].transform.Find("Informations/Name/Money/MoneyText").gameObject;
        if (money != null)
        {
            money.GetComponent<TMP_Text>().text = amount.ToString() +" $";
        }
    }

    // Update is called once per frame
    void Update()
    {
        // P�tla po wszystkich graczach, �eby zaktualizowa� ich wy�wietlane informacje
        // (na razie tylko o zak�adach i posiadanych �etonach)
        // TODO doda� tu aktualizowanie wy�wietlania kart na stoliku i u graczy
        int i = 0;
        
        foreach (KeyValuePair<string, PlayerState> state in this.playersStates)
        {
            PlayerState playerState = state.Value;
            
            // Je�li to g��wny gracz, to mamy od tego osobne zmienne
            // TODO mo�na by to zmieni�, ale nwm, mo�e tak w sumie wygodniej?
            if (playerState.Nick == MyGameManager.Instance.MainPlayer.Nick)
            {
                this.InfoMainPlayerName.text = playerState.Nick;
                this.InfoMainPlayerChips.text = Convert.ToString(playerState.TokensCount) + " $";
                this.InfoMainPlayerBid.text = "Bet\n" + Convert.ToString(playerState.CurrentBet) + " $";
                this.ShowMainPlayerCards(playerState.Hand); // karty g��wnego gracza
                continue;
            }

            this.ShowPlayerOnTable(i, playerState.Nick);
            this.ChangePlayerBet(playerState.CurrentBet, i);
            this.ChangePlayerMoney(playerState.TokensCount, i);
            this.ShowPlayerCards(i, playerState.Hand); // karty wsp�graczy
            i++;
        }
   
        // Wy�wietlanie kart na stoliku
        if (this.gameTableState.Cards != null)
        {
            for (int j = 0; j < this.gameTableState.Cards.Cards.Count; j++)
                ShowCardOnDeck(this.gameTableState.Cards.Cards[j], j);
        }
        else
            HideCardsOnDeck();

        if (this.gameTableState.TokensInGame == 0)
            DeleteChipsBitInGame();
        else
            UpdateChipsBidInGame(this.gameTableState.TokensInGame);


        // Wy�wietlanie Popupu o kolejno�ci ruchu
        if (this.displayPlayerTurnPopup && PopupWindow)
        {
            ShowMenu(true);
            Vector3 position = new Vector3(660.0f, 490.0f, 0.0f);
            var popup = Instantiate(PopupWindow, position, Quaternion.identity, transform);
            popup.GetComponent<TextMeshProUGUI>().text = "It's your turn, make a move";
            this.displayPlayerTurnPopup = false;
        }
        
        // Wy�wietlanie Popupu o zwyci�zcy gry
        if (this.displayWinnerPopup && PopupWindow)
        {
            var popup = Instantiate(PopupWindow, transform.position, Quaternion.identity, transform);
            popup.GetComponent<TextMeshProUGUI>().text = "And the winner is:\n" + this.winnerNick + "\nCongrats!";
            this.displayWinnerPopup = false;
        }
    }

    // Wczytanie stawki z pola input 'Bid'
    public void ReadInputBet(string inputBet)
    {
        if (inputBet.Length == 0)
        {
            this.betFieldText = null;
            return;
        }
        this.betFieldText = inputBet;
        Debug.Log(this.betFieldText);
    }

    // Obs�uga przycisk�w z menu ruch�w,
    // wysy�anie odpowiednich zapyta� do serwera w ka�dym z nich
    // TODO wysy�anie ��da� mo�na ewentualnie przenie�� do osobnej klasy
    public void OnCheckButton()
    {
        Debug.Log("Check");
        if(this.readyToSendMove)
        {
            NetworkStream gameStream = MyGameManager.Instance.gameServerConnection.stream;
            NetworkHelper.WriteNetworkStream(gameStream, "1 ");
            this.readyToSendMove = false;
            ShowMenu(false);
        }
    }
    public void OnAllInButton()
    {
        Debug.Log("All in");
        if (this.readyToSendMove)
        {
            NetworkStream gameStream = MyGameManager.Instance.gameServerConnection.stream;
            NetworkHelper.WriteNetworkStream(gameStream, "3 ");
            this.readyToSendMove = false;
            ShowMenu(false);
        }
    }
    public void OnPassButton()
    {
        Debug.Log("Pass");
        if (this.readyToSendMove)
        {
            NetworkStream gameStream = MyGameManager.Instance.gameServerConnection.stream;
            NetworkHelper.WriteNetworkStream(gameStream, "0 ");
            this.readyToSendMove = false;
            ShowMenu(false);
        }
    }
    // TODO (cz. PGGP-106) doda� sprawdzanie, czy podali�my jaki� zak�ad w polu input 'Bid' i czy to liczba,
    // bo aktualnie podajemy po prostu string
    public void OnBidButton()
    {
        Debug.Log("Bid");
        if (this.readyToSendMove)
        {
            NetworkStream gameStream = MyGameManager.Instance.gameServerConnection.stream;
            NetworkHelper.WriteNetworkStream(gameStream, "2 " + this.betFieldText.ToString());
            this.readyToSendMove = false;
            ShowMenu(false);
        }
    }

    // Wysy�anie zapytania do serwera o rozpocz�cie gry
    // TODO mo�e przenie�� kiedy� do osobnej klasy
    public void onStartGameButton()
    {
        string token = MyGameManager.Instance.clientToken;
        byte[] toSend = System.Text.Encoding.ASCII.GetBytes(token + ' ' + "6" + ' ');
        MyGameManager.Instance.mainServerConnection.stream.Write(toSend, 0, toSend.Length);
        MyGameManager.Instance.mainServerConnection.stream.Flush();
        Thread.Sleep(1000);
    }
}
