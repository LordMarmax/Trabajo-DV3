using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    public List<CardData> TodasLasCartas = new List<CardData>();
    public List<CardData> Mazo = new List<CardData>();
    public List<CardData> Mercado = new List<CardData>();
    public int RondaActual = 1;
    private Random rng = new Random();

    private HBoxContainer mercadoUI;
    private Label textoConsola;
    private PackedScene cartaVisualScene;
    
    // NUEVO: Referencia al manager de losetas
    public TileManager ManagerDeLosetas;

    // Conseciones.
    public ConcesionesVerdes ManagerVerdes;
    public ConcesionesRojas ManagerRojas;

    public override void _Ready()
    {
        CargarCartasConstruccion();
        
        // NUEVO: Instanciamos y cargamos las losetas desde el código de tu amigo
        ManagerDeLosetas = new TileManager();
        AddChild(ManagerDeLosetas); // Lo agregamos al árbol de Godot
        ManagerDeLosetas.CargarLosetas();


        //Incializar las Concesiones verdes
        ManagerVerdes = new ConcesionesVerdes();
        AddChild(ManagerVerdes);
        ManagerVerdes.CargarCartas();

        //Inicializar las Concesiones rojas
        ManagerRojas = new ConcesionesRojas();
        AddChild(ManagerRojas);
        ManagerRojas.CargarCartas();
        /*Nota:
            El GameManager podrá acceder a los datos mediante:
                
                ConcessionData verde = ManagerVerdes.ObtenerCarta("Carta_001");
        */


        cartaVisualScene = GD.Load<PackedScene>("res://escenas/CartaVisual.tscn");
        mercadoUI = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/MercadoUI");
        textoConsola = GetNode<Label>("/root/Mundo/InterfazJuego/TextoConsola");
        
        IniciarPartida();
    }

    private void IniciarPartida()
    {
        Mazo = TodasLasCartas.OrderBy(x => rng.Next()).ToList();
        
        for (int i = 0; i < 3; i++)
        {
            RobarCartaAlMercado();
        }
        
        ImprimirMercado();
    }

    private void RobarCartaAlMercado()
    {
        if (Mazo.Count > 0)
        {
            Mercado.Add(Mazo[0]);
            Mazo.RemoveAt(0);
        }
    }

    private void SeleccionarCarta(int indexCartaJugador)
    {
        if (Mercado.Count < 3) return;

        CardData cartaJugador = Mercado[indexCartaJugador];
        
        GD.Print("=========================================");
        GD.Print($"RONDA {RondaActual} - TÚ TOMAS: {cartaJugador.CardId} (Nivel {cartaJugador.Level})");
        
        // Imprimimos la lista de espacios que acabamos de guardar en variables
        GD.Print("   DATOS ALMACENADOS EN MEMORIA:");
        foreach (var espacio in cartaJugador.Spaces)
        {
            string estado = espacio.IsOccupied ? "OCUPADO" : "LIBRE";
            GD.Print($"   -> [{estado}] Pos: {espacio.Position,-10} | Forma: {espacio.Shape,-5} | Contenido: {espacio.Content}");
        }
        
        int indexBot = (indexCartaJugador != 2) ? 2 : 1;
        CardData cartaBot = Mercado[indexBot];
        GD.Print($"[AUTÓMATA] Harrington & Sons toma: {cartaBot.CardId} (Nivel {cartaBot.Level})");
        
        int indexDescartada = 3 - indexCartaJugador - indexBot; 
        GD.Print($"[DESCARTE] Se va a la pila: {Mercado[indexDescartada].CardId}");
        GD.Print("=========================================\n");
        
        if (textoConsola != null)
        {
            textoConsola.Text = $"Tomaste: {cartaJugador.CardId} (Nivel {cartaJugador.Level})";
        }

        Mercado.Clear();
        for (int i = 0; i < 3; i++)
        {
            RobarCartaAlMercado();
        }

        RondaActual++;

        if (RondaActual <= 12)
        {
            ImprimirMercado();
        }
        else
        {
            if (textoConsola != null) textoConsola.Text = "FIN DEL JUEGO";
        }
    }

    private void ImprimirMercado()
    {
        if (mercadoUI != null)
        {
            foreach (Node hijo in mercadoUI.GetChildren()) hijo.QueueFree();

            for (int i = 0; i < Mercado.Count; i++)
            {
                TextureButton nuevaCartaVisual = cartaVisualScene.Instantiate<TextureButton>();
                
                if (nuevaCartaVisual != null)
                {
                    nuevaCartaVisual.TextureNormal = Mercado[i].CardImage;
                    nuevaCartaVisual.CustomMinimumSize = new Vector2(180, 250); 
                    nuevaCartaVisual.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    nuevaCartaVisual.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                    nuevaCartaVisual.IgnoreTextureSize = true; 
                    nuevaCartaVisual.StretchMode = TextureButton.StretchModeEnum.Scale;
                    
                    int indexActual = i; 
                    nuevaCartaVisual.Pressed += () => SeleccionarCarta(indexActual);
                    
                    mercadoUI.AddChild(nuevaCartaVisual);
                }
            }
        }
    }

    private void CargarCartasConstruccion()
    {
        string rutaBase = "res://assets/cartas_construcción/";
        
        Dictionary<string, (int Level, string Desc)> mapaCartas = new Dictionary<string, (int, string)>
        {
            {"carta(1)", (9, "Ocup: Astro(Inf-Der) | Libre: 2x1V(Der), 1x1(Sup-Izq)")},
            {"carta(2)", (8, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(3)", (8, "Ocup: Hábitat ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(4)", (8, "Ocup: Cráter ⚫(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
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
            {"carta(21)", (1, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(22)", (1, "Ocup: Invernadero S(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(23)", (1, "Ocup: Astro(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(24)", (1, "Ocup: Cráter ⚫(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(25)", (1, "Ocup: Hábitat ❤️(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(26)", (10, "Ocup: Agua ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(27)", (10, "Ocup: Invernadero ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(28)", (10, "Ocup: Astro(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(29)", (10, "Ocup: Agua ❤️(Sup-Der) | Libre: 1x1(Sup-Izq), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(30)", (10, "Ocup: Invernadero ❤️(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
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
            {"carta(43)", (6, "Ocup: Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
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
            {"carta(58)", (1, "Ocup: Invernadero ❤️(Sup-Izq) | Libre: 1x1(Sup-Der), 1x1(Inf-Izq), 1x1(Inf-Der)")},
            {"carta(59)", (1, "Ocup: Satélite S(Inf-Izq) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Der)")},
            {"carta(60)", (10, "Ocup: Cráter ⚫(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
            {"carta(61)", (10, "Ocup: Invernadero ❤️(Inf-Der) | Libre: 1x1(Sup-Izq), 1x1(Sup-Der), 1x1(Inf-Izq)")},
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