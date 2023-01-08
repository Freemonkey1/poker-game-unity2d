﻿using System;

namespace PokerGameClasses
{
    class Program
    {
        static void Main(string[] args)
        {
            Card sampleCard = new Card(CardSign.Heart, CardValue.Ten);
            Console.WriteLine(sampleCard.Name);
            Console.WriteLine(sampleCard.CardColorToString());

            Console.WriteLine();

            CardsCollection sampleDeck = CardsCollection.CreateStandardDeck();
            sampleDeck.Cards.ForEach(c => Console.WriteLine(c.Name));
            Console.WriteLine();
            Card takenCard = sampleDeck.TakeOutCard(CardSign.Heart, CardValue.Ace);
            sampleDeck.Cards.ForEach(c => Console.WriteLine(c.Name));
            Console.WriteLine();
            Console.WriteLine(takenCard.Name);
            Console.WriteLine(sampleDeck.TakeOutCard(CardSign.Heart, CardValue.Ace));
            sampleDeck.Cards.ForEach(c => Console.WriteLine(c.Name));

            Console.WriteLine();

            sampleDeck.ShuffleCards();
            sampleDeck.Cards.ForEach(c => Console.WriteLine(c.Name));
            
        }
    }
}
