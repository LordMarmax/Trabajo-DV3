        using Godot;
        using System;
        using System.Collections.Generic;

        [GlobalClass]
        public partial class CardData : Resource
        {
            [Export] public string CardId { get; set; }
            [Export] public int Level { get; set; }
            [Export] public string Description { get; set; }
            [Export] public Texture2D CardImage { get; set; }
            
            // NUEVO: Aquí guardaremos los datos estructurados de la geometría
            public List<CardSpace> Spaces = new List<CardSpace>();
        }

        // Nueva clase para definir cada hueco de la carta
        public class CardSpace
        {
            public string Position { get; set; } // "Sup-Izq", "Inf-Der", "Der", etc.
            public string Shape { get; set; }    // "1x1", "2x1H", "2x1V"
            public string Content { get; set; }  // "Empty", "Astro", "Cráter", etc.
            public bool IsOccupied { get; set; } // true si está ocupado, false si está libre

            public CardSpace(string position, string shape, string content, bool isOccupied)
            {
                Position = position;
                Shape = shape;
                Content = content;
                IsOccupied = isOccupied;
            }
        }