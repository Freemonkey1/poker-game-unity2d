using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using System;
using System.Text;


namespace PokerGameClasses
{
    public class PlayerState
    {
        public string Nick { get; set; }
        public CardsCollection Hand { get; set; }
        public int TokensCount { get; set; }
        public int CurrentBet { get; set; }
        public int Xp { get; set; }

        public PlayerState()
        {
            this.Nick = null;
            this.Hand = null;
            this.TokensCount = 0;
            this.CurrentBet = 0;
            this.Xp = 0;
        }

        public PlayerState(string nick, CardsCollection hand, int tokensCount, int currentBet, int xp)
        {
            this.Nick = nick;
            this.Hand = hand;
            this.TokensCount = tokensCount;
            this.CurrentBet = currentBet;
            this.Xp = xp;
        }

        public void UnpackGameState(string[] splitted)
        {
            string[] playerState = splitted[1].Split(new string(":"));
            this.Nick = playerState[2];
            string hand = playerState[4];
            CardsCollection cardsCollection = CardsHelper.StringToCardsCollection(hand);
            this.Hand = cardsCollection;
            this.TokensCount = Convert.ToInt32(playerState[6]);
            this.CurrentBet = Convert.ToInt32(playerState[8]);
            this.Xp = Convert.ToInt32(playerState[10]);
            
        }
        //TODO mo�e doda� lustrzan� metod� PackGameState? B�dziemy wysy�a� tak� wiadomo�� do serwera wgl?

        public override string ToString()
        {
            return "Player's '" + this.Nick + "' game state:" +
                "\nHand: " + this.Hand +
                "\nTokens: " + this.TokensCount +
                "\nCurrent Bet: " + this.CurrentBet +
                "\nXP: " + this.Xp + "\n";
        }
    }
}
