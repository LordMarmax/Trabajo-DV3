using Godot;
using System;
using System.Collections.Generic;

public partial class CardManager : Node
{
    public List<CardData> TodasLasCartas = new List<CardData>();

    public void CargarCartas()
    {
        string rutaBase = "res://assets/cartas_construcción/";
        Dictionary<string, (int Level, string Desc)> mapaCartas = new Dictionary<string, (int, string)>
        {
            {"carta(1)", (9, "Ocup: Astro(Inf-Der) | Libre: 2x1V(Izq), 1x1(Sup-Der)")},
            {"carta(2)", (8, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(3)", (8, "Ocup: Hábitat ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(4)", (8, "Ocup: Cráter ⚫(Inf-Izq) | Libre: 1x1(Sup-Der), 1x1(Sup-Izq), 1x1(Inf-Der)")},//hasta aquí 
            {"carta(5)", (8, "Ocup: Edif.Azul ❤️(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(6)", (7, "Ocup: Cráter ⚫(Sup-Izq), Astro(Sup-Der) | Libre: 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(7)", (7, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(8)", (7, "Ocup: Astro(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(9)", (6, "Ocup: Astro(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(10)", (4, "Ocup: Cráter ⚫(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(11)", (4, "Ocup: Astro(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(12)", (3, "Ocup: Edif.Azul ❤️(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(13)", (3, "Ocup: Astro(Inf-Izq), Invernadero ❤️(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der)")},
            {"carta(14)", (3, "Ocup: Astro(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(15)", (2, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(16)", (2, "Ocup: Edif.Azul ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(17)", (2, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(18)", (2, "Ocup: Astro ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(19)", (2, "Ocup: Hábitat ❤️(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(20)", (1, "Ocup: Agua ❤️(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(21)", (1, "Ocup: Cráter ⚫(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(22)", (1, "Ocup: Invernadero S(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(23)", (1, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(24)", (1, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(25)", (1, "Ocup: Hábitat ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(26)", (10, "Ocup: Agua ❤️(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(27)", (10, "Ocup: Invernadero ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(28)", (10, "Ocup: Astro(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(29)", (10, "Ocup: Agua ❤️(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(30)", (10, "Ocup: Invernadero ❤️(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(31)", (9, "Ocup: Cráter ⚫(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(32)", (9, "Ocup: Hábitat ❤️(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(33)", (9, "Ocup: Invernadero S(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(34)", (9, "Ocup: Cráter ⚫(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(35)", (9, "Ocup: Astro(Inf-Izq) | Libre: 2x1H(Sup), 1x1(Inf-Der)")},
            {"carta(36)", (9, "Ocup: Astro(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(37)", (9, "Ocup: Edif.Azul ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(38)", (8, "Ocup: Astro(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(39)", (8, "Ocup: Astro(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(40)", (8, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(41)", (7, "Ocup: Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(42)", (6, "Ocup: Cráter ⚫(Sup-Izq), Astro(Inf-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(43)", (6, "Ocup: Cráter ⚫(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(44)", (5, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(45)", (5, "Ocup: Astro(Sup-Der), Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq)")},
            {"carta(46)", (4, "Ocup: Astro(Inf-Izq), Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der)")},
            {"carta(47)", (3, "Ocup: Astro(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(48)", (3, "Ocup: Cráter ⚫(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(49)", (3, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(50)", (3, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(51)", (2, "Ocup: Invernadero ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(52)", (2, "Ocup: Astro(Inf-Der) | Libre: 2x1H(Sup), 1x1(Inf-Izq)")},
            {"carta(53)", (2, "Ocup: Cráter ⚫(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(54)", (2, "Ocup: Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(55)", (1, "Ocup: Astro(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(56)", (1, "Ocup: Cráter ⚫(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(57)", (1, "Ocup: Agua ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(58)", (1, "Ocup: Invernadero ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(59)", (1, "Ocup: Satélite S(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(60)", (10, "Ocup: Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(61)", (10, "Ocup: Invernadero ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(62)", (10, "Ocup: Satélite S(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(63)", (10, "Ocup: Cráter ⚫(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(64)", (10, "Ocup: Astro(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(65)", (9, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")}
        };
        
        foreach (var item in mapaCartas)
        {
            Texture2D textura = GD.Load<Texture2D>(rutaBase + item.Key + ".png");
            if (textura != null)
            {
                CardData nuevaCarta = new CardData();
                nuevaCarta.CardId = item.Key;
                nuevaCarta.Level = item.Value.Level;
                nuevaCarta.Description = item.Value.Desc;
                nuevaCarta.CardImage = textura;
                
                ProcesarEspaciosCarta(nuevaCarta);
                
                TodasLasCartas.Add(nuevaCarta);
            }
        }
    }

    private void ProcesarEspaciosCarta(CardData carta)
    {
        string desc = carta.Description;
        string[] partes = desc.Split(" | ");

        if (partes[0].StartsWith("Ocup: "))
        {
            string ocupados = partes[0].Replace("Ocup: ", "");
            string[] items = ocupados.Split(", ");
            foreach (var item in items)
            {
                int abre = item.IndexOf('(');
                int cierra = item.IndexOf(')');
                if (abre != -1 && cierra != -1)
                {
                    string contenido = item.Substring(0, abre).Trim();
                    string posicion = item.Substring(abre + 1, cierra - abre - 1);
                    carta.Spaces.Add(new CardSpace(posicion, "1x1", contenido, true));
                }
            }
        }

        if (partes.Length > 1 && partes[1].StartsWith("Libre: "))
        {
            string libres = partes[1].Replace("Libre: ", "");
            string[] items = libres.Split(", ");
            foreach (var item in items)
            {
                int abre = item.IndexOf('(');
                int cierra = item.IndexOf(')');
                if (abre != -1 && cierra != -1)
                {
                    string forma = item.Substring(0, abre).Trim();
                    string posicion = item.Substring(abre + 1, cierra - abre - 1);
                    carta.Spaces.Add(new CardSpace(posicion, forma, "Empty", false));
                }
            }
        }
    }
}