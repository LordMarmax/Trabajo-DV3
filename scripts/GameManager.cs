using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    public List<CardData> Mazo = new List<CardData>();
    public List<CardData> Mercado = new List<CardData>();
    
    public List<CardData> Fila1 = new List<CardData>();
    public List<CardData> Fila2 = new List<CardData>();
    public List<CardData> Fila3 = new List<CardData>();
    
    public List<TileData> ManoLosetas = new List<TileData>();
    
    public int RondaActual = 1;
    private Random rng = new Random();

    private HBoxContainer mercadoUI;
    private GridContainer manoLosetasUI;
    private Label textoConsola;
    private PackedScene cartaVisualScene;
    private PackedScene losetaVisualScene;
    
    private HBoxContainer filaUI1;
    private HBoxContainer filaUI2;
    private HBoxContainer filaUI3;
    
    private Control vistaMercado;
    private Control vistaTablero;
    private Button btnIrTablero;
    private Button btnVolverMercado;
    
    private Control DropFila1;
    private Control DropFila2;
    private Control DropFila3;
    private CanvasLayer DragLayer;
    
    // Variables Públicas para que CardVisual y LosetaVisual puedan consultarlas
    public Control ItemArrastrado = null;
    private Control DummyOriginal = null;
    private Vector2 OffsetArrastre;
    private Control PadreOriginal;
    private Vector2 PosicionOriginal;
    private object DataArrastrada;

    public CardManager ManagerDeCartas;
    public TileManager ManagerDeLosetas;
    public ConcesionesVerdes ManagerVerdes;
    public ConcesionesRojas ManagerRojas;

    public override void _Ready()
    {
        ManagerDeCartas = new CardManager();
        AddChild(ManagerDeCartas);
        ManagerDeCartas.CargarCartas();
        
        ManagerDeLosetas = new TileManager();
        AddChild(ManagerDeLosetas);
        ManagerDeLosetas.CargarLosetas();

        ManagerVerdes = new ConcesionesVerdes();
        AddChild(ManagerVerdes);
        ManagerVerdes.CargarCartas();

        ManagerRojas = new ConcesionesRojas();
        AddChild(ManagerRojas);
        ManagerRojas.CargarCartas();

        cartaVisualScene = GD.Load<PackedScene>("res://escenas/CartaVisual.tscn");
        losetaVisualScene = GD.Load<PackedScene>("res://escenas/LosetaVisual.tscn");
        
        vistaMercado = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado");
        vistaTablero = GetNode<Control>("/root/Mundo/InterfazJuego/VistaTableroPersonal");
        
        mercadoUI = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaMercado/MercadoUI");
        manoLosetasUI = GetNode<GridContainer>("/root/Mundo/InterfazJuego/VistaMercado/ManoLosetasUI");
        textoConsola = GetNode<Label>("/root/Mundo/InterfazJuego/VistaMercado/TextoConsola");
        
        btnIrTablero = GetNode<Button>("/root/Mundo/InterfazJuego/VistaMercado/BtnIrTablero");
        btnVolverMercado = GetNode<Button>("/root/Mundo/InterfazJuego/VistaTableroPersonal/BtnVolverMercado");
        DragLayer = GetNode<CanvasLayer>("/root/Mundo/DragLayer");

        DropFila1 = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/DropFila1");
        DropFila2 = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/DropFila2");
        DropFila3 = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/DropFila3");

        btnIrTablero.Pressed += () => CambiarVista(true);
        btnVolverMercado.Pressed += () => CambiarVista(false);

        filaUI1 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila1");
        filaUI2 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila2");
        filaUI3 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila3");
        
        CambiarVista(false);
        IniciarPartida();
    }

    public void IniciarArrastre(Control item, Control padre, Vector2 posOriginal, object data, Vector2 localMousePos)
    {
        if (ItemArrastrado != null) return; // Seguridad extra

        ItemArrastrado = item;
        PadreOriginal = padre;
        PosicionOriginal = posOriginal;
        DataArrastrada = data;

        DummyOriginal = new Control();
        DummyOriginal.CustomMinimumSize = item.CustomMinimumSize;
        DummyOriginal.SizeFlagsHorizontal = item.SizeFlagsHorizontal;
        DummyOriginal.SizeFlagsVertical = item.SizeFlagsVertical;

        int index = item.GetIndex();
        padre.RemoveChild(item);
        padre.AddChild(DummyOriginal);
        padre.MoveChild(DummyOriginal, index);

        DragLayer.AddChild(item);
        
        if (item is CardVisual) item.CustomMinimumSize = new Vector2(209, 271);
        else if (item is LosetaVisual) item.CustomMinimumSize = new Vector2(100, 100);

        OffsetArrastre = localMousePos;
        item.GlobalPosition = GetViewport().GetMousePosition() - OffsetArrastre;
        
        Input.SetDefaultCursorShape(Input.CursorShape.Drag);
    }

    public override void _Process(double delta)
    {
        if (ItemArrastrado != null)
        {
            // Si el ratón ya NO está presionado, procesamos el drop inmediatamente
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                ProcesarSoltar();
                return;
            }

            ItemArrastrado.GlobalPosition = GetViewport().GetMousePosition() - OffsetArrastre;
            
            Vector2 mousePos = GetViewport().GetMousePosition();
            bool esCarta = DataArrastrada is CardData;
            bool overValid = false;

            if (esCarta)
            {
                if (DropFila1.GetGlobalRect().HasPoint(mousePos)) overValid = true;
                else if (DropFila2.GetGlobalRect().HasPoint(mousePos)) overValid = true;
                else if (DropFila3.GetGlobalRect().HasPoint(mousePos)) overValid = true;
            }

            if (overValid)
                Input.SetDefaultCursorShape(Input.CursorShape.Drag); 
            else
                Input.SetDefaultCursorShape(Input.CursorShape.Forbidden); 
        }
    }

    private void ProcesarSoltar()
    {
        Vector2 mousePos = GetViewport().GetMousePosition();
        int filaDestino = -1;

        if (DataArrastrada is CardData)
        {
            if (DropFila1.GetGlobalRect().HasPoint(mousePos)) filaDestino = 1;
            else if (DropFila2.GetGlobalRect().HasPoint(mousePos)) filaDestino = 2;
            else if (DropFila3.GetGlobalRect().HasPoint(mousePos)) filaDestino = 3;
        }

        if (filaDestino != -1)
        {
            CardData carta = (CardData)DataArrastrada;
            if (ValidarColocacion(carta, filaDestino))
            {
                var itemParaDestruir = ItemArrastrado;
                ItemArrastrado = null; 
                
                if (DummyOriginal != null) { DummyOriginal.QueueFree(); DummyOriginal = null; }
                itemParaDestruir.QueueFree();

                SoltarCartaEnFila(carta, filaDestino);
            }
            else
            {
                DevolverCarta();
            }
        }
        else
        {
            DevolverCarta();
        }
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
    }

    private bool ValidarColocacion(CardData carta, int fila)
    {
        List<CardData> filaElegida = (fila == 1) ? Fila1 : (fila == 2) ? Fila2 : Fila3;
        if (filaElegida.Count > 0)
        {
            CardData ultimaCarta = filaElegida.Last();
            if (carta.Level < ultimaCarta.Level)
            {
                if (textoConsola != null)
                {
                    textoConsola.Text = "¡Error! Orden ascendente incorrecto.";
                    Tween tweenError = CreateTween();
                    tweenError.TweenProperty(textoConsola, "modulate", Colors.Red, 0.1f);
                    tweenError.TweenProperty(textoConsola, "modulate", Colors.White, 0.2f);
                }
                return false;
            }
        }
        return true;
    }

    private void DevolverCarta()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(ItemArrastrado, "global_position", DummyOriginal.GlobalPosition, 0.25f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        
        tween.TweenCallback(Callable.From(() => {
            if (ItemArrastrado == null) return; // Seguridad por si algo lo borró

            ItemArrastrado.GetParent().RemoveChild(ItemArrastrado);
            DummyOriginal.GetParent().AddChild(ItemArrastrado);
            DummyOriginal.GetParent().MoveChild(ItemArrastrado, DummyOriginal.GetIndex());
            
            ItemArrastrado.GlobalPosition = PosicionOriginal;
            
            DummyOriginal.QueueFree();
            DummyOriginal = null;
            ItemArrastrado = null;
        }));
    }

    private void CambiarVista(bool mostrarTablero)
    {
        vistaMercado.Visible = !mostrarTablero;
        vistaTablero.Visible = mostrarTablero;
    }

    private void IniciarPartida()
    {
        Mazo = ManagerDeCartas.TodasLasCartas.OrderBy(x => rng.Next()).ToList();
        LlenarMercado();
        ImprimirMercado();
        ImprimirManoLosetas();
    }

    private void LlenarMercado()
    {
        Mercado.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (Mazo.Count > 0)
            {
                Mercado.Add(Mazo[0]);
                Mazo.RemoveAt(0);
            }
        }
    }

    private void RobarLosetas()
    {
        ManoLosetas.Clear();
        int cantidadLosetas = (RondaActual - 1) % 4 + 1;
        
        for (int i = 0; i < cantidadLosetas; i++)
        {
            if (ManagerDeLosetas.TodasLasLosetas.Count > 0)
            {
                int randomIndex = rng.Next(ManagerDeLosetas.TodasLasLosetas.Count);
                ManoLosetas.Add(ManagerDeLosetas.TodasLasLosetas[randomIndex]);
            }
        }
    }

    public void SoltarCartaEnFila(CardData cartaArrastrada, int numeroFila)
    {
        GD.Print("=========================================");
        GD.Print($"[TÚ] Tomas la carta: {cartaArrastrada.CardId} (Nivel {cartaArrastrada.Level})");
        GD.Print("=========================================\n");
        
        Mercado.Remove(cartaArrastrada);
        ImprimirMercado();
        
        EjecutarAnimacionColocacion(cartaArrastrada, numeroFila);
    }

    private void EjecutarAnimacionColocacion(CardData carta, int numeroFila)
    {
        Tween tweenEspera = CreateTween();
        tweenEspera.TweenInterval(0.1f);
        tweenEspera.TweenCallback(Callable.From(() => {
            CambiarVista(true);
            
            if (numeroFila == 1) Fila1.Add(carta);
            else if (numeroFila == 2) Fila2.Add(carta);
            else Fila3.Add(carta);
            
            ActualizarInterfazTablero(carta);
            
            HBoxContainer filaActual = (numeroFila == 1) ? filaUI1 : (numeroFila == 2) ? filaUI2 : filaUI3;
            Control wrapper = (Control)filaActual.GetChild(filaActual.GetChildCount() - 1);
            TextureRect imagenReal = (TextureRect)wrapper.GetChild(0);
            
            Tween tweenCrecimiento = CreateTween();
            tweenCrecimiento.TweenProperty(imagenReal, "scale", Vector2.One, 0.4f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            
            tweenCrecimiento.TweenInterval(0.5f);
            tweenCrecimiento.TweenCallback(Callable.From(() => {
                SiguienteRonda();
            }));
        }));
    }

    private void SiguienteRonda()
    {
        if (Mercado.Count > 0)
        {
            CardData cartaBot = Mercado.Last();
            GD.Print("=========================================");
            GD.Print($"[AUTÓMATA] Harrington & Sons toma: {cartaBot.CardId} (Nivel {cartaBot.Level})");
            GD.Print("=========================================\n");
            Mercado.Remove(cartaBot);
        }
        
        LlenarMercado();
        RondaActual++;
        
        if (Mercado.Count == 0)
        {
            if (textoConsola != null) textoConsola.Text = "FIN DEL JUEGO (Mazo vacío)";
            CambiarVista(false);
            return;
        }

        RobarLosetas();
        ImprimirMercado();
        ImprimirManoLosetas();
        
        if (textoConsola != null) textoConsola.Text = $"Ronda {RondaActual} / 12 - Arrastra una carta a tu tablero.";
        CambiarVista(false);
    }

    private void ActualizarInterfazTablero(CardData cartaNueva = null)
    {
        foreach (Node hijo in filaUI1.GetChildren()) if (hijo is Control && !(hijo is HBoxContainer)) hijo.QueueFree();
        foreach (Node hijo in filaUI2.GetChildren()) if (hijo is Control && !(hijo is HBoxContainer)) hijo.QueueFree();
        foreach (Node hijo in filaUI3.GetChildren()) if (hijo is Control && !(hijo is HBoxContainer)) hijo.QueueFree();

        DibujarCartasEnFila(Fila1, filaUI1, cartaNueva);
        DibujarCartasEnFila(Fila2, filaUI2, cartaNueva);
        DibujarCartasEnFila(Fila3, filaUI3, cartaNueva);
    }

    private void DibujarCartasEnFila(List<CardData> lista, HBoxContainer filaUI, CardData cartaNueva)
    {
        foreach (CardData carta in lista)
        {
            Control wrapper = new Control();
            wrapper.CustomMinimumSize = new Vector2(130, 180);

            TextureRect visual = new TextureRect();
            visual.Texture = carta.CardImage;
            visual.CustomMinimumSize = new Vector2(130, 180);
            visual.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            visual.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered; 
            visual.PivotOffset = new Vector2(65, 90); 
            
            if (carta == cartaNueva)
                visual.Scale = Vector2.Zero; 
            else
                visual.Scale = Vector2.One;

            wrapper.AddChild(visual);
            filaUI.AddChild(wrapper);
        }
    }

    private void ImprimirMercado()
    {
        if (mercadoUI != null)
        {
            foreach (Node hijo in mercadoUI.GetChildren()) 
            {
                if (hijo is CardVisual) hijo.QueueFree();
            }

            for (int i = 0; i < Mercado.Count; i++)
            {
                CardVisual nuevaCartaVisual = cartaVisualScene.Instantiate<CardVisual>();
                nuevaCartaVisual.TextureNormal = Mercado[i].CardImage;
                nuevaCartaVisual.Data = Mercado[i];
                
                nuevaCartaVisual.CustomMinimumSize = new Vector2(209, 271); 
                nuevaCartaVisual.Size = new Vector2(209, 271);
                nuevaCartaVisual.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
                nuevaCartaVisual.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
                
                nuevaCartaVisual.IgnoreTextureSize = true; 
                nuevaCartaVisual.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
                nuevaCartaVisual.CardIndex = i;
                
                mercadoUI.AddChild(nuevaCartaVisual);
            }
        }
    }

    private void ImprimirManoLosetas()
    {
        if (manoLosetasUI != null)
        {
            foreach (Node hijo in manoLosetasUI.GetChildren()) hijo.QueueFree();

            for (int i = 0; i < ManoLosetas.Count; i++)
            {
                LosetaVisual nuevaLoseta = losetaVisualScene.Instantiate<LosetaVisual>();
                nuevaLoseta.TextureNormal = ManoLosetas[i].TileImage;
                nuevaLoseta.Data = ManoLosetas[i];
                
                nuevaLoseta.CustomMinimumSize = new Vector2(100, 100);
                nuevaLoseta.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                nuevaLoseta.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                
                nuevaLoseta.IgnoreTextureSize = true;
                nuevaLoseta.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered; 
                nuevaLoseta.TileIndex = i;

                manoLosetasUI.AddChild(nuevaLoseta);
            }
        }
    }
}