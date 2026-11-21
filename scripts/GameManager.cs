using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    // Managers
    public CardManager ManagerDeCartas;
    public TileManager ManagerDeLosetas;
    public ConcesionesVerdes ManagerVerdes;
    public ConcesionesRojas ManagerRojas;

    // Estado del Juego
    public int FaseActual = 1;
    public int TurnoActual = 1;
    public int MazoCartasIndex = 0;
    
    // Listas del Juego
    public List<CardData> MazoCartas = new List<CardData>();
    public List<(CardData Carta, TileData Loseta)> Mercado = new List<(CardData, TileData)>();
    
    // Tus filas personales
    public List<CardData> Fila1 = new List<CardData>();
    public List<CardData> Fila2 = new List<CardData>();
    public List<CardData> Fila3 = new List<CardData>();
    
    // Losetas en mano
    public List<TileData> ManoLosetas = new List<TileData>();

    // Nodos UI
    private HBoxContainer mercadoUI;
    private GridContainer manoLosetasUI;
    private Label textoConsola;
    
    // Columnas del Mercado
    private Control col1Carta, col1Loseta;
    private Control col2Carta, col2Loseta;
    private Control col3Carta, col3Loseta;

    // Concesiones
    private TextureRect concesionLargo, concesionCorto1, concesionCorto2;

    // Tablero Central (12 Espacios)
    private TextureRect[] espaciosTablero = new TextureRect[12];

    // Nodos de Tablero Personal
    private HBoxContainer filaUI1, filaUI2, filaUI3;
    private Control DropFila1, DropFila2, DropFila3;
    private Control vistaMercado, vistaTablero;
    private Button btnIrTablero, btnVolverMercado;
    private Button btnFase1, btnFase2, btnFase3;
    private CanvasLayer DragLayer;

    // Lógica de Arrastre
    public Control ItemArrastrado = null;
    private Control DummyOriginal = null;
    private Vector2 OffsetArrastre;
    private Control PadreOriginal;
    private Vector2 PosicionOriginal;
    private object DataArrastrada;

    private PackedScene cartaVisualScene;
    private PackedScene losetaVisualScene;
    private Random rng = new Random();

    public override void _Ready()
    {
        // Inicializar Managers
        ManagerDeCartas = new CardManager(); AddChild(ManagerDeCartas); ManagerDeCartas.CargarCartas();
        ManagerDeLosetas = new TileManager(); AddChild(ManagerDeLosetas); ManagerDeLosetas.CargarLosetas("A");
        ManagerVerdes = new ConcesionesVerdes(); AddChild(ManagerVerdes); ManagerVerdes.CargarCartas();
        ManagerRojas = new ConcesionesRojas(); AddChild(ManagerRojas); ManagerRojas.CargarCartas();

        cartaVisualScene = GD.Load<PackedScene>("res://escenas/CartaVisual.tscn");
        losetaVisualScene = GD.Load<PackedScene>("res://escenas/LosetaVisual.tscn");

        // Encontrar Nodos
        vistaMercado = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado");
        vistaTablero = GetNode<Control>("/root/Mundo/InterfazJuego/VistaTableroPersonal");
        
        mercadoUI = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaMercado/MercadoUI");
        
        col1Carta = mercadoUI.GetNode<Control>("Columna1/SlotCarta");
        col1Loseta = mercadoUI.GetNode<Control>("Columna1/SlotLoseta");
        col2Carta = mercadoUI.GetNode<Control>("Columna2/SlotCarta");
        col2Loseta = mercadoUI.GetNode<Control>("Columna2/SlotLoseta");
        col3Carta = mercadoUI.GetNode<Control>("Columna3/SlotCarta");
        col3Loseta = mercadoUI.GetNode<Control>("Columna3/SlotLoseta");

        // NOTA: ManoLosetasUI debe estar en VistaTableroPersonal
        manoLosetasUI = GetNode<GridContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ManoLosetasUI");
        textoConsola = GetNode<Label>("/root/Mundo/InterfazJuego/VistaMercado/TextoConsola");
        
        // Concesiones
        concesionLargo = GetNode<TextureRect>("/root/Mundo/InterfazJuego/VistaMercado/ConcesionesUI/ConcesionLargo");
        concesionCorto1 = GetNode<TextureRect>("/root/Mundo/InterfazJuego/VistaMercado/ConcesionesUI/ConcesionCorto1");
        concesionCorto2 = GetNode<TextureRect>("/root/Mundo/InterfazJuego/VistaMercado/ConcesionesUI/ConcesionCorto2");

        // Tablero Central (12 espacios)
        Control tableroCentral = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/TableroLosetasUI");
        for (int i = 0; i < 12; i++)
        {
            espaciosTablero[i] = tableroCentral.GetNode<TextureRect>($"Espacio{i + 1}");
            espaciosTablero[i].Texture = null; // Limpiar al inicio
        }

        btnFase1 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase1");
        btnFase2 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase2");
        btnFase3 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase3");
        
        btnIrTablero = GetNode<Button>("/root/Mundo/InterfazJuego/VistaMercado/BtnIrTablero");
        btnVolverMercado = GetNode<Button>("/root/Mundo/InterfazJuego/VistaTableroPersonal/BtnVolverMercado");
        
        DragLayer = GetNode<CanvasLayer>("/root/Mundo/DragLayer");

        DropFila1 = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/DropFila1");
        DropFila2 = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/DropFila2");
        DropFila3 = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/DropFila3");

        filaUI1 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila1");
        filaUI2 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila2");
        filaUI3 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila3");

        // Conectar Botones
        btnFase1.Pressed += () => IniciarFase(1);
        btnFase2.Pressed += () => IniciarFase(2);
        btnFase3.Pressed += () => IniciarFase(3);
        btnIrTablero.Pressed += () => CambiarVista(true);
        btnVolverMercado.Pressed += () => CambiarVista(false);

        // Iniciar Juego
        CambiarVista(false);
        IniciarFase(1);
    }

    public void IniciarFase(int fase)
    {
        FaseActual = fase;
        TurnoActual = 1;
        string letraFase = (fase == 1) ? "A" : (fase == 2) ? "B" : "C";
        
        ManagerDeLosetas.CargarLosetas(letraFase);
        
        MazoCartas = ManagerDeCartas.TodasLasCartas.OrderBy(x => rng.Next()).ToList();
        MazoCartasIndex = 0;

        CargarConcesiones();
        PrepararTurno();
    }

    private void CargarConcesiones()
    {
        // Buscamos en los managers del amigo y ponemos las imágenes
        var rojas = ManagerRojas.Cartas.Values.ToList();
        var verdes = ManagerVerdes.Cartas.Values.ToList();

        if (rojas.Count > 0) concesionLargo.Texture = rojas[rng.Next(rojas.Count)].ConcessionImage;
        if (verdes.Count > 0) concesionCorto1.Texture = verdes[rng.Next(verdes.Count)].ConcessionImage;
        if (verdes.Count > 1) concesionCorto2.Texture = verdes[rng.Next(verdes.Count)].ConcessionImage;
    }

    public void PrepararTurno()
    {
        GD.Print($"--- FASE {FaseActual} - TURNO {TurnoActual} ---");
        
        Mercado.Clear();
        int cantidadLosetas = TurnoActual; // 1, 2, 3 o 4
        
        // 1. Llenar el mercado de 3 columnas
        for (int i = 0; i < 3; i++)
        {
            if (MazoCartasIndex < MazoCartas.Count && ManagerDeLosetas.TodasLasLosetas.Count > 0)
            {
                CardData carta = MazoCartas[MazoCartasIndex];
                MazoCartasIndex++;
                
                int randomTile = rng.Next(ManagerDeLosetas.TodasLasLosetas.Count);
                TileData loseta = ManagerDeLosetas.TodasLasLosetas[randomTile];
                
                Mercado.Add((carta, loseta));
            }
        }

        // 2. Actualizar UI
        ImprimirMercado();
        ActualizarTableroCentral(cantidadLosetas);
    }

    private void ImprimirMercado()
    {
        LimpiarSlot(col1Carta); LimpiarSlot(col1Loseta);
        LimpiarSlot(col2Carta); LimpiarSlot(col2Loseta);
        LimpiarSlot(col3Carta); LimpiarSlot(col3Loseta);

        if (Mercado.Count > 0) MostrarEnSlot(col1Carta, col1Loseta, Mercado[0], 0);
        if (Mercado.Count > 1) MostrarEnSlot(col2Carta, col2Loseta, Mercado[1], 1);
        if (Mercado.Count > 2) MostrarEnSlot(col3Carta, col3Loseta, Mercado[2], 2);
    }

    private void MostrarEnSlot(Control slotCarta, Control slotLoseta, (CardData Carta, TileData Loseta) par, int index)
    {
        CardVisual cv = cartaVisualScene.Instantiate<CardVisual>();
        cv.TextureNormal = par.Carta.CardImage;
        cv.Data = par.Carta;
        cv.CardIndex = index;
        cv.CustomMinimumSize = new Vector2(180, 240);
        cv.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
        slotCarta.AddChild(cv);

        LosetaVisual lv = losetaVisualScene.Instantiate<LosetaVisual>();
        lv.TextureNormal = par.Loseta.TileImage;
        lv.Data = par.Loseta;
        lv.CustomMinimumSize = new Vector2(100, 100);
        lv.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
        slotLoseta.AddChild(lv);
    }

    private void LimpiarSlot(Control slot)
    {
        foreach (Node hijo in slot.GetChildren()) hijo.QueueFree();
    }

    private void ActualizarTableroCentral(int cantidad)
    {
        // Limpiamos todos los espacios primero
        for (int i = 0; i < 12; i++) espaciosTablero[i].Texture = null;

        // Según la cantidad de losetas del turno, las ponemos en los espacios
        for (int i = 0; i < cantidad; i++)
        {
            if (ManagerDeLosetas.TodasLasLosetas.Count > 0)
            {
                int randomTile = rng.Next(ManagerDeLosetas.TodasLasLosetas.Count);
                espaciosTablero[i].Texture = ManagerDeLosetas.TodasLasLosetas[randomTile].TileImage;
            }
        }
    }

    // --- ARRASTRE ---
    public void IniciarArrastre(Control item, Control padre, Vector2 posOriginal, object data, Vector2 localMousePos)
    {
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
            if (!Input.IsMouseButtonPressed(MouseButton.Left)) { ProcesarSoltar(); return; }
            ItemArrastrado.GlobalPosition = GetViewport().GetMousePosition() - OffsetArrastre;
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
            // Buscar el par completo (Carta, Loseta)
            var par = Mercado.FirstOrDefault(x => x.Carta == carta);
            
            if (par.Carta != null)
            {
                ItemArrastrado.QueueFree();
                if (DummyOriginal != null) { DummyOriginal.QueueFree(); DummyOriginal = null; }
                ItemArrastrado = null;

                // Aquí llamarías a una función para colocar la carta en el tablero
                // y la loseta en la mano. (Lo haremos en el siguiente paso).
                GD.Print($"Colocando carta {carta.CardId} y loseta {par.Loseta.TileId}");
            }
        }
        else
        {
            DevolverCarta();
        }
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
    }

    private void DevolverCarta()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(ItemArrastrado, "global_position", DummyOriginal.GlobalPosition, 0.25f);
        tween.TweenCallback(Callable.From(() => {
            ItemArrastrado.GetParent().RemoveChild(ItemArrastrado);
            DummyOriginal.GetParent().AddChild(ItemArrastrado);
            DummyOriginal.GetParent().MoveChild(ItemArrastrado, DummyOriginal.GetIndex());
            ItemArrastrado.GlobalPosition = PosicionOriginal;
            DummyOriginal.QueueFree(); DummyOriginal = null; ItemArrastrado = null;
        }));
    }

    private void CambiarVista(bool mostrarTablero)
    {
        vistaMercado.Visible = !mostrarTablero;
        vistaTablero.Visible = mostrarTablero;
    }
}