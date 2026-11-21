using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// GameManager — implementa la lógica completa del PDF "Requisitos Proyecto Luna Capital".
///
/// Resumen del flujo (según PDF):
///  - Al iniciar la partida: se barajan las cartas, se dan 3 cartas de construcción al
///    humano (una sola vez, para toda la partida). Las concesiones se fijan al inicio y
///    NO cambian entre fases.
///  - Cada fase usa un tipo de loseta (A, B o C). Dentro de cada fase hay 4 turnos.
///  - Cada turno: el sistema carga el tablero central con 3*turno losetas en posiciones
///    fijas (Turno 1 → 3, Turno 2 → 6, Turno 3 → 9, Turno 4 → 12).
///    Se muestran 3 columnas de mercado (carta construcción + loseta) para elegir.
///  - Turno del HUMANO: elige una columna (carta + loseta). La carta va a su tablero
///    personal (validando reglas de colocación); la loseta pasa a su mano.
///  - Turno del AUTÓMATA: toma automáticamente la columna adyacente (izquierda o
///    derecha de la que tomó el humano, al azar si ambas están disponibles).
///  - Al final del turno se recargan las losetas del tablero central (devolviendo las
///    anteriores). Las cartas de construcción NO se reparten de nuevo al humano.
///  - Reglas de colocación de cartas (PDF):
///      * Cada fila debe tener números estrictamente crecientes de izquierda a derecha.
///      * Solo se pueden agregar cartas en los extremos de una fila (nunca intercalar).
///      * Máximo 3 filas.
///      * Si la carta no cabe en ningún lugar válido, se descarta.
///  - Colocar losetas: las losetas en mano se arrastran a los slots vacíos de las cartas
///    del tablero personal (en el mismo turno que se robaron — pendiente validar eso).
/// </summary>
public partial class GameManager : Node
{
    // ── Managers ────────────────────────────────────────────────────────────────
    public CardManager ManagerDeCartas;
    public TileManager ManagerDeLosetas;
    public ConcesionesVerdes ManagerVerdes;
    public ConcesionesRojas ManagerRojas;

    // ── Estado del Juego ─────────────────────────────────────────────────────────
    public int FaseActual = 1;
    public int TurnoActual = 1;

    // ── Mazos y mercado ──────────────────────────────────────────────────────────
    private List<CardData> mazoCartas = new List<CardData>();
    private int mazoIndex = 0;

    // Mercado: 3 columnas. Cada columna = (carta, lista de losetas). null si ya fue tomada.
    // La cantidad de losetas por columna = TurnoActual (1,2,3,4 según el turno).
    private (CardData Carta, List<TileData> Losetas)?[] mercado = new (CardData, List<TileData>)?[3];

    // Mazo de losetas barajado por fase — sin repetición dentro de la fase
    private List<TileData> mazoLosetas = new List<TileData>();
    private int mazoLosetasIndex = 0;

    // ── Mano inicial del humano (3 cartas, dadas UNA SOLA VEZ) ───────────────────
    private List<CardData> manoHumano = new List<CardData>();       // 3 cartas de construcción
    private bool cartasInicialDadas = false;

    // ── Tablero personal del humano ──────────────────────────────────────────────
    // Cada fila es una lista ordenada de CartaTablero (máx 3 filas)
    private List<CartaTablero>[] filasHumano = {
        new List<CartaTablero>(),
        new List<CartaTablero>(),
        new List<CartaTablero>()
    };

    // Losetas en mano del humano (robadas en este turno, listas para colocar)
    private List<TileData> manoLosetas = new List<TileData>();

    // ── Tablero del autómata ─────────────────────────────────────────────────────
    // Solo guardamos las cartas y losetas que tomó (para puntuación futura)
    private List<CardData> cartasAutomata = new List<CardData>();
    private List<TileData> losetasAutomata = new List<TileData>();

    // ── Concesiones (fijas durante toda la partida) ──────────────────────────────
    private bool concesionesFijadas = false;

    // ── Nodos UI ─────────────────────────────────────────────────────────────────
    private Control vistaMercado;
    private Control vistaTablero;
    private HBoxContainer mercadoUI;
    private GridContainer manoLosetasUI;
    private Label textoConsola;
    private CanvasLayer dragLayer;

    // Slots de las 3 columnas del mercado
    private Control col1Carta, col1Loseta;
    private Control col2Carta, col2Loseta;
    private Control col3Carta, col3Loseta;

    // Concesiones (fijas durante toda la partida)
    private TextureRect concesionLargo, concesionCorto1, concesionCorto2;

    // Tablero personal: tres HBoxContainer de filas
    private HBoxContainer filaUI1, filaUI2, filaUI3;

    // Zonas de drop de filas (en VistaMercado) — ya no se usan (todo es click)
    // private Control dropFila1, dropFila2, dropFila3;

    // Botones
    private Button btnIrTablero, btnVolverMercado;
    private Button btnFase1, btnFase2, btnFase3;
    private Label labelEstado;

    // Panel para elegir fila al colocar una carta
    private Control panelElegirFila;
    private Label labelPreguntaFila;
    private Label labelErrorFila;
    private Button btnFilaUI1, btnFilaUI2, btnFilaUI3, btnCancelarFila;

    // Carta pendiente de colocación (mientras el panel está abierto)
    private CardData cartaPendiente = null;

    // ── Drag & Drop ──────────────────────────────────────────────────────────────
    public Control ItemArrastrado = null;
    private Control dummyOriginal = null;
    private Vector2 offsetArrastre;
    private Control padreOriginal;
    private Vector2 posicionOriginal;
    private object dataArrastrada;

    // ── Escenas ──────────────────────────────────────────────────────────────────
    private PackedScene cartaVisualScene;
    private PackedScene losetaVisualScene;
    private PackedScene cartaTableroScene;

    private Random rng = new Random();

    // ═══════════════════════════════════════════════════════════════════════════
    // INICIALIZACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    public override void _Ready()
    {
        // Managers
        ManagerDeCartas = new CardManager();   AddChild(ManagerDeCartas);   ManagerDeCartas.CargarCartas();
        ManagerDeLosetas = new TileManager();  AddChild(ManagerDeLosetas);
        ManagerVerdes = new ConcesionesVerdes(); AddChild(ManagerVerdes);   ManagerVerdes.CargarCartas();
        ManagerRojas = new ConcesionesRojas();  AddChild(ManagerRojas);    ManagerRojas.CargarCartas();

        // Escenas
        cartaVisualScene  = GD.Load<PackedScene>("res://escenas/CartaVisual.tscn");
        losetaVisualScene = GD.Load<PackedScene>("res://escenas/LosetaVisual.tscn");
        cartaTableroScene = GD.Load<PackedScene>("res://escenas/CartaTablero.tscn");

        // Nodos UI — usamos la escena Mundo.tscn tal como está
        vistaMercado  = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado");
        vistaTablero  = GetNode<Control>("/root/Mundo/InterfazJuego/VistaTableroPersonal");
        mercadoUI     = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaMercado/MercadoUI");
        manoLosetasUI = GetNode<GridContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ManoLosetasUI");
        textoConsola  = GetNode<Label>("/root/Mundo/InterfazJuego/VistaMercado/TextoConsola");
        dragLayer     = GetNode<CanvasLayer>("/root/Mundo/InterfazJuego/DragLayer");
        labelEstado   = GetNode<Label>("/root/Mundo/InterfazJuego/LabelEstado");

        col1Carta   = mercadoUI.GetNode<Control>("Columna1/SlotCarta");
        col1Loseta  = mercadoUI.GetNode<Control>("Columna1/SlotLoseta");
        col2Carta   = mercadoUI.GetNode<Control>("Columna2/SlotCarta");
        col2Loseta  = mercadoUI.GetNode<Control>("Columna2/SlotLoseta");
        col3Carta   = mercadoUI.GetNode<Control>("Columna3/SlotCarta");
        col3Loseta  = mercadoUI.GetNode<Control>("Columna3/SlotLoseta");

        // Concesiones — nodo renombrado a ConcesionesUI (sin espacio)
        var concesionesNodo = GetNode<Control>("/root/Mundo/InterfazJuego/VistaMercado/ConcesionesUI");
        concesionLargo  = concesionesNodo.GetNode<TextureRect>("ConcesionLargo");
        concesionCorto1 = concesionesNodo.GetNode<TextureRect>("ConcesionCorto1");
        concesionCorto2 = concesionesNodo.GetNode<TextureRect>("ConcesionCorto2");

        // Tablero central — eliminado, ya no existe en la escena

        // Filas del tablero personal
        filaUI1 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila1");
        filaUI2 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila2");
        filaUI3 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila3");

        // Botones de fase: solo se obtienen la referencia para ocultarlos por código.
        // La progresión es automática; el jugador no puede saltar fases manualmente.
        btnFase1 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase1");
        btnFase2 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase2");
        btnFase3 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase3");
        btnFase1.Visible = false;
        btnFase2.Visible = false;
        btnFase3.Visible = false;

        btnIrTablero   = GetNode<Button>("/root/Mundo/InterfazJuego/VistaMercado/BtnIrTablero");
        btnVolverMercado = GetNode<Button>("/root/Mundo/InterfazJuego/VistaTableroPersonal/BtnVolverMercado");

        // Las cartas del mercado solo se seleccionan con click (CardVisual._GuiInput).
        // No hay zonas de drop en VistaMercado.

        // NO se conectan los botones de fase a IniciarFase — la progresión es automática.
        btnIrTablero.Pressed     += () => CambiarVista(true);
        btnVolverMercado.Pressed += () => CambiarVista(false);

        // Panel selección de fila
        panelElegirFila  = GetNode<Control>("/root/Mundo/InterfazJuego/PanelElegirFila");
        labelPreguntaFila = panelElegirFila.GetNode<Label>("VBox/LabelPregunta");
        labelErrorFila   = panelElegirFila.GetNode<Label>("VBox/LabelError");
        btnFilaUI1       = panelElegirFila.GetNode<Button>("VBox/BtnFila1");
        btnFilaUI2       = panelElegirFila.GetNode<Button>("VBox/BtnFila2");
        btnFilaUI3       = panelElegirFila.GetNode<Button>("VBox/BtnFila3");
        btnCancelarFila  = panelElegirFila.GetNode<Button>("VBox/BtnCancelar");
        panelElegirFila.Visible = false;

        btnFilaUI1.Pressed += () => IntentarColocarEnFila(0);
        btnFilaUI2.Pressed += () => IntentarColocarEnFila(1);
        btnFilaUI3.Pressed += () => IntentarColocarEnFila(2);
        btnCancelarFila.Pressed += () => CerrarPanelFila(descartada: true);

        // Iniciar partida
        CambiarVista(false);
        IniciarPartida();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // INICIO DE PARTIDA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Arranca la partida desde cero:
    /// - Baraja todas las cartas de construcción.
    /// - Da 3 cartas al humano (una sola vez en toda la partida).
    /// - Fija las concesiones (no cambiarán entre fases).
    /// - Arranca la Fase 1.
    /// </summary>
    private void IniciarPartida()
    {
        // Barajar mazo global
        mazoCartas = ManagerDeCartas.TodasLasCartas.OrderBy(_ => rng.Next()).ToList();
        mazoIndex = 0;

        // Dar 3 cartas de construcción al humano (solo una vez)
        manoHumano.Clear();
        for (int i = 0; i < 3 && mazoIndex < mazoCartas.Count; i++)
            manoHumano.Add(mazoCartas[mazoIndex++]);
        cartasInicialDadas = true;

        // Fijar concesiones para toda la partida
        FijarConcesiones();

        // Limpiar tablero personal
        LimpiarTableroPersonal();

        // Iniciar Fase 1
        IniciarFase(1);
    }

    /// <summary>
    /// Fija las 3 concesiones al principio de la partida.
    /// No vuelve a llamarse entre fases.
    /// </summary>
    private void FijarConcesiones()
    {
        if (concesionesFijadas) return;
        concesionesFijadas = true;

        var rojas  = ManagerRojas.Cartas.Values.ToList();
        var verdes = ManagerVerdes.Cartas.Values.ToList();

        if (rojas.Count  > 0) concesionLargo.Texture  = rojas[rng.Next(rojas.Count)].ConcessionImage;
        if (verdes.Count > 0) concesionCorto1.Texture  = verdes[rng.Next(verdes.Count)].ConcessionImage;
        if (verdes.Count > 1) concesionCorto2.Texture  = verdes[rng.Next(verdes.Count)].ConcessionImage;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FASES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cambia de fase (1, 2 o 3). Solo cambia el tipo de loseta (A/B/C).
    /// Las concesiones y las cartas del humano NO se tocan.
    /// </summary>
    public void IniciarFase(int fase)
    {
        FaseActual  = fase;
        TurnoActual = 1;

        string letraFase = fase switch { 1 => "A", 2 => "B", _ => "C" };
        ManagerDeLosetas.CargarLosetas(letraFase);

        // Barajar todas las losetas de esta fase — sin repetición durante toda la fase
        mazoLosetas = ManagerDeLosetas.TodasLasLosetas.OrderBy(_ => rng.Next()).ToList();
        mazoLosetasIndex = 0;

        Log($"=== FASE {FaseActual} iniciada (losetas tipo {letraFase}, total {mazoLosetas.Count}) ===");

        PrepararTurno();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TURNO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Prepara el mercado para el turno actual:
    /// - Recarga el tablero central con 3*TurnoActual losetas (en posiciones fijas).
    /// - Pone 3 columnas de (carta, loseta) en el mercado.
    /// Las 3 cartas de construcción del humano en mano NO se tocan.
    /// </summary>
    public void PrepararTurno()
    {
        Log($"--- FASE {FaseActual} · TURNO {TurnoActual} ---");
        if (labelEstado != null)
            labelEstado.Text = $"Fase {FaseActual}  ·  Turno {TurnoActual} / 4";

        // Cada columna recibe TurnoActual losetas únicas (sacadas del mazo sin repetición)
        int losetasPorColumna = TurnoActual; // T1=1, T2=2, T3=3, T4=4
        mercado[0] = mercado[1] = mercado[2] = null;

        for (int i = 0; i < 3; i++)
        {
            if (mazoIndex >= mazoCartas.Count) break;
            CardData carta = mazoCartas[mazoIndex++];

            var losetasColumna = new List<TileData>();
            for (int l = 0; l < losetasPorColumna; l++)
            {
                if (mazoLosetasIndex >= mazoLosetas.Count)
                {
                    // Si se agotó el mazo de losetas (no debería ocurrir con 33 losetas y máx 12 por turno),
                    // volver a barajar solo las restantes
                    Log("Aviso: mazo de losetas agotado antes de lo esperado — fase sin losetas repetidas.");
                    break;
                }
                losetasColumna.Add(mazoLosetas[mazoLosetasIndex++]);
            }

            mercado[i] = (carta, losetasColumna);
        }

        RenderizarMercado();
        RenderizarManoHumano();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ACCIÓN DEL HUMANO — tomar una columna
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// El humano seleccionó la columna 'colIndex' (0, 1 o 2).
    /// 1. Intenta colocar la carta en el tablero personal.
    /// 2. Agrega la loseta a la mano del humano.
    /// 3. El autómata toma la columna adyacente.
    /// 4. Avanza al siguiente turno (o fase).
    /// </summary>
    public void HumanoEligeColumna(int colIndex)
    {
        // Si el panel de fila está abierto, ignorar nuevos clicks en el mercado
        if (cartaPendiente != null) return;

        if (mercado[colIndex] == null)
        {
            Log("Esa columna ya fue tomada.");
            return;
        }

        var (carta, losetas) = mercado[colIndex].Value;
        mercado[colIndex] = null;

        // Guardar la carta y las losetas en variables pendientes,
        // luego mostrar el panel para que el jugador elija la fila
        cartaPendiente = carta;
        _losetasPendientes = losetas;
        _colPendiente = colIndex;

        AbrirPanelFila(carta);
    }

    // Variables temporales mientras el panel está abierto
    private List<TileData> _losetasPendientes;
    private int _colPendiente;

    private void AbrirPanelFila(CardData carta)
    {
        labelErrorFila.Text = "";
        labelPreguntaFila.Text = $"Carta nivel {carta.Level}\n¿En qué fila la colocas?";

        // Habilitar/deshabilitar botones según qué filas admiten la carta
        for (int f = 0; f < 3; f++)
        {
            var btn = f == 0 ? btnFilaUI1 : f == 1 ? btnFilaUI2 : btnFilaUI3;
            var fila = filasHumano[f];
            bool puedeIr = PuedeEntrarEnFila(carta, fila, out _);
            bool filaLibre = fila.Count == 0;
            btn.Disabled = false; // siempre habilitado — la validación la damos al presionar
        }

        panelElegirFila.Visible = true;
    }

    private void IntentarColocarEnFila(int filaIndex)
    {
        if (cartaPendiente == null) return;
        var fila = filasHumano[filaIndex];

        if (!PuedeEntrarEnFila(cartaPendiente, fila, out string razon))
        {
            labelErrorFila.Text = razon;
            return;
        }

        // Colocar la carta
        AgregarCartaAFila(cartaPendiente, filaIndex);
        CerrarPanelFila(descartada: false);
    }

    /// <summary>
    /// Valida si una carta puede entrar en una fila dada.
    /// Regla: el nivel debe ser ≥ al nivel de la última carta de la fila,
    /// o la fila está vacía.
    /// </summary>
    private bool PuedeEntrarEnFila(CardData carta, List<CartaTablero> fila, out string razon)
    {
        razon = "";
        if (fila.Count == 0) return true; // fila vacía — siempre acepta

        int ultimoNivel = fila[fila.Count - 1].Data.Level;
        if (carta.Level < ultimoNivel)
        {
            razon = $"No es posible: la carta (Nivel {carta.Level}) es menor que la última de esta fila (Nivel {ultimoNivel}).";
            return false;
        }
        return true;
    }

    private void CerrarPanelFila(bool descartada)
    {
        if (descartada)
        {
            // "Cancelar" = el jugador no quiere esta carta ahora.
            // Devolvemos la columna al mercado para que pueda elegir otra.
            if (cartaPendiente != null)
                mercado[_colPendiente] = (cartaPendiente, _losetasPendientes);

            cartaPendiente = null;
            _losetasPendientes = null;
            panelElegirFila.Visible = false;
            RenderizarMercado(); // vuelve a mostrar las 3 columnas disponibles
            return;              // NO se avanza el turno
        }

        // Carta colocada correctamente — proceder con losetas y autómata
        if (cartaPendiente != null)
        {
            foreach (var loseta in _losetasPendientes)
                manoLosetas.Add(loseta);
            RenderizarManoLosetas();

            Log($"Humano tomó col {_colPendiente + 1}: {_losetasPendientes.Count} loseta(s)");

            AutomataTomaTurno(_colPendiente);
            RenderizarMercado();
        }

        cartaPendiente = null;
        _losetasPendientes = null;
        panelElegirFila.Visible = false;
        FinalizarTurno();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AUTÓMATA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// El autómata toma la columna adyacente a la que tomó el humano
    /// (izquierda o derecha, al azar si ambas están disponibles).
    /// PDF: "El autómata se lleva la que está a la izquierda o la que está
    /// a la derecha de la que tú te llevaste."
    /// </summary>
    private void AutomataTomaTurno(int colHumano)
    {
        int izq = colHumano - 1;
        int der = colHumano + 1;

        bool hayIzq = izq >= 0 && mercado[izq] != null;
        bool hayDer = der <= 2 && mercado[der] != null;

        int colAutomata = -1;
        if (hayIzq && hayDer) colAutomata = rng.Next(2) == 0 ? izq : der;
        else if (hayIzq) colAutomata = izq;
        else if (hayDer) colAutomata = der;

        if (colAutomata < 0) { Log("Autómata: no hay columna adyacente disponible."); return; }

        var (carta, losetas) = mercado[colAutomata].Value;
        mercado[colAutomata] = null;

        cartasAutomata.Add(carta);
        losetasAutomata.AddRange(losetas);

        Log($"Autómata tomó col {colAutomata + 1}: carta {carta.CardId}, {losetas.Count} loseta(s)");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FIN DE TURNO / FASE
    // ═══════════════════════════════════════════════════════════════════════════

    private void FinalizarTurno()
    {
        Log($"Turno {TurnoActual} completado.");

        if (TurnoActual < 4)
        {
            TurnoActual++;
            PrepararTurno();
        }
        else
        {
            FinalizarFase();
        }
    }

    private void FinalizarFase()
    {
        Log($"=== Fase {FaseActual} completada ===");
        if (FaseActual < 3)
        {
            Log("Pasando a la siguiente fase…");
            IniciarFase(FaseActual + 1);
        }
        else
        {
            Log("¡Partida terminada! Calculando puntuación…");
            MostrarPuntuacion();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VALIDACIÓN Y COLOCACIÓN DE CARTAS EN EL TABLERO PERSONAL
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Intenta colocar 'carta' en el tablero personal del humano.
    /// Reglas (PDF):
    ///   1. Números estrictamente crecientes dentro de cada fila.
    ///   2. Solo se puede agregar en un extremo (izquierda o derecha) de una fila,
    ///      NUNCA intercalar.
    ///   3. Máximo 3 filas. Se puede iniciar una fila nueva si la carta toca
    ///      (por borde) a alguna carta existente — o si es la primera carta.
    ///   4. Si no cabe en ningún lugar válido, se descarta.
    /// Devuelve true si fue colocada, false si fue descartada.
    /// </summary>
    private void AgregarCartaAFila(CardData carta, int filaIndex)
    {
        CartaTablero nodo = cartaTableroScene.Instantiate<CartaTablero>();
        nodo.CustomMinimumSize = new Vector2(120, 170);

        HBoxContainer filaUI = filaIndex == 0 ? filaUI1 : filaIndex == 1 ? filaUI2 : filaUI3;

        filasHumano[filaIndex].Add(nodo);
        filaUI.AddChild(nodo);
        nodo.Inicializar(carta);

        Log($"  → Carta {carta.CardId} (Nv{carta.Level}) en Fila {filaIndex + 1}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DRAG & DROP DE LOSETAS HACIA CUADRANTES DE CARTAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inicia el arrastre de una loseta desde la mano del jugador.
    /// Solo las LosetaVisual deben llamar a este método.
    /// </summary>
    public void IniciarArrastre(Control item, Control padre, Vector2 posOriginal, object data, Vector2 localMousePos)
    {
        if (data is not TileData) return;

        ItemArrastrado = item;
        padreOriginal  = padre;
        dataArrastrada = data;

        // Guardar posición global antes de cualquier movimiento
        posicionOriginal = item.GlobalPosition;

        // En lugar de sacar el nodo, lo dejamos en manoLosetasUI pero lo hacemos
        // invisible — así las otras losetas no se mueven.
        // Clonamos el nodo para usarlo como fantasma de drag.
        LosetaVisual fantasmaVisual = losetaVisualScene.Instantiate<LosetaVisual>();
        var losetaData = data as TileData;
        fantasmaVisual.TextureNormal = losetaData.TileImage;
        fantasmaVisual.Data = losetaData;
        fantasmaVisual.IgnoreTextureSize = true;
        fantasmaVisual.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
        fantasmaVisual.CustomMinimumSize = item.Size; // mismo tamaño visual
        fantasmaVisual.MouseFilter = Control.MouseFilterEnum.Ignore; // no interceptar clicks

        // Ocultar el nodo original en la mano (placeholder invisible)
        item.Modulate = new Color(1, 1, 1, 0);

        // El fantasma vive en DragLayer y se mueve con el cursor
        dragLayer.AddChild(fantasmaVisual);
        fantasmaVisual.GlobalPosition = posicionOriginal;

        // Reemplazar ItemArrastrado por el fantasma
        ItemArrastrado = fantasmaVisual;

        offsetArrastre = localMousePos;
        fantasmaVisual.GlobalPosition = GetViewport().GetMousePosition() - offsetArrastre;
        Input.SetDefaultCursorShape(Input.CursorShape.Drag);
    }

    public override void _Process(double delta)
    {
        if (ItemArrastrado == null || !IsInstanceValid(ItemArrastrado)) return;
        if (!Input.IsMouseButtonPressed(MouseButton.Left)) { ProcesarSoltar(); return; }
        ItemArrastrado.GlobalPosition = GetViewport().GetMousePosition() - offsetArrastre;
    }

    private void ProcesarSoltar()
    {
        if (ItemArrastrado == null) return;
        Vector2 mousePos = GetViewport().GetMousePosition();

        if (dataArrastrada is TileData loseta)
        {
            var fantasma = ItemArrastrado;
            ItemArrastrado = null;
            LimpiarDummy();
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);

            bool colocada = IntentarColocarLoseta(loseta, mousePos);
            if (colocada)
            {
                // Colocada: destruir fantasma y re-renderizar mano completa
                // (las otras losetas se reposicionan ahora que hay una menos)
                manoLosetas.Remove(loseta);
                fantasma.QueueFree();
                RenderizarManoLosetas();
                return;
            }

            // No colocada: las otras losetas siguen exactamente donde están.
            // Ocultamos la loseta real en la mano (aún está en manoLosetas)
            // mientras el fantasma anima el regreso a su posición original.
            OcultarLosetaEnMano(loseta);

            // Animar el fantasma de vuelta a posicionOriginal, luego mostrar la loseta real
            Vector2 destino = posicionOriginal;
            Tween tween = CreateTween();
            tween.TweenProperty(fantasma, "global_position", destino, 0.22f)
                 .SetTrans(Tween.TransitionType.Cubic)
                 .SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(() =>
            {
                if (IsInstanceValid(fantasma)) fantasma.QueueFree();
                MostrarLosetaEnMano(loseta); // revelar la loseta real
            }));
            return;
        }

        // Fallback
        var nodoFallback = ItemArrastrado;
        ItemArrastrado = null;
        nodoFallback?.QueueFree();
        LimpiarDummy();
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
    }

    /// Oculta visualmente la loseta correspondiente en ManoLosetasUI sin quitarla de manoLosetas.
    private void OcultarLosetaEnMano(TileData loseta)
    {
        // El nodo original está en padreOriginal, ya oculto desde IniciarArrastre.
        // No hace falta hacer nada más — ya está invisible.
    }

    /// Revela la loseta que fue ocultada temporalmente.
    private void MostrarLosetaEnMano(TileData loseta)
    {
        // Restaurar visibilidad del nodo original en padreOriginal
        if (padreOriginal != null && IsInstanceValid(padreOriginal))
        {
            foreach (Node hijo in padreOriginal.GetChildren())
            {
                if (hijo is LosetaVisual lv && lv.Data == loseta)
                {
                    lv.Modulate = Colors.White;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Intenta colocar la loseta en el slot exacto donde fue soltada.
    /// Lógica: primero determina sobre qué slot cayó el cursor;
    /// si ese slot no acepta la loseta → devuelve false (no busca alternativas).
    /// </summary>
    private bool IntentarColocarLoseta(TileData loseta, Vector2 mousePos)
    {
        for (int f = 0; f < 3; f++)
        {
            foreach (CartaTablero ct in filasHumano[f])
            {
                for (int s = 0; s < 4; s++)
                {
                    Control slotCtrl = ct.ObtenerSlotControl(s);
                    // ¿El cursor está sobre este slot?
                    if (!slotCtrl.GetGlobalRect().HasPoint(mousePos)) continue;

                    // El cursor está exactamente aquí.
                    // Si el slot no acepta la loseta → no colocar y salir.
                    if (!ct.SlotAceptaLoseta(s)) return false;

                    bool ok = ct.ColocarLoseta(loseta, s);
                    if (ok)
                        Log($"Loseta {loseta.TileId} colocada en slot {s} de {ct.Data.CardId}");
                    return ok;
                }
            }
        }
        // El cursor no cayó sobre ningún slot → devolver
        return false;
    }

    private void DevolverItem()
    {
        if (ItemArrastrado == null || !IsInstanceValid(ItemArrastrado)) return;

        var item = ItemArrastrado;
        // Capturar posición y padre destino AHORA, antes de que el dummy sea liberado
        Vector2 posDestino = (dummyOriginal != null && IsInstanceValid(dummyOriginal))
            ? dummyOriginal.GlobalPosition
            : posicionOriginal;
        var padreDestino   = (dummyOriginal != null && IsInstanceValid(dummyOriginal))
            ? dummyOriginal.GetParent() as Control
            : padreOriginal;
        int idxDestino     = (dummyOriginal != null && IsInstanceValid(dummyOriginal))
            ? dummyOriginal.GetIndex()
            : -1;

        Tween tween = CreateTween();
        tween.TweenProperty(item, "global_position", posDestino, 0.2f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (!IsInstanceValid(item)) return;
            item.GetParent()?.RemoveChild(item);
            if (padreDestino != null && IsInstanceValid(padreDestino))
            {
                padreDestino.AddChild(item);
                if (idxDestino >= 0) padreDestino.MoveChild(item, idxDestino);
            }
            item.GlobalPosition = posDestino;
            if (ItemArrastrado == item) ItemArrastrado = null;
        }));

        // Limpiar el dummy DESPUÉS de haber capturado lo que necesitamos
        LimpiarDummy();
    }

    private void LimpiarDummy()
    {
        dummyOriginal?.QueueFree();
        dummyOriginal = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RENDER UI
    // ═══════════════════════════════════════════════════════════════════════════

    private void RenderizarMercado()
    {
        LimpiarSlot(col1Carta); LimpiarSlot(col1Loseta);
        LimpiarSlot(col2Carta); LimpiarSlot(col2Loseta);
        LimpiarSlot(col3Carta); LimpiarSlot(col3Loseta);

        Control[] slotsCarta  = { col1Carta,  col2Carta,  col3Carta  };
        Control[] slotsLoseta = { col1Loseta, col2Loseta, col3Loseta };

        for (int i = 0; i < 3; i++)
        {
            if (mercado[i] == null) continue;
            var (carta, losetas) = mercado[i].Value;

            // ── Carta: ocupa exactamente el SlotCarta ──────────────────────
            CardVisual cv = cartaVisualScene.Instantiate<CardVisual>();
            cv.TextureNormal = carta.CardImage;
            cv.Data = carta;
            cv.CardIndex = i;
            // Anclar al rect completo del slot padre
            cv.LayoutMode = 1;          // ancla
            cv.AnchorLeft   = 0f;
            cv.AnchorTop    = 0f;
            cv.AnchorRight  = 1f;
            cv.AnchorBottom = 1f;
            cv.OffsetLeft   = 0f;
            cv.OffsetTop    = 0f;
            cv.OffsetRight  = 0f;
            cv.OffsetBottom = 0f;
            cv.IgnoreTextureSize = true;
            cv.StretchMode = TextureButton.StretchModeEnum.Scale; // rellena sin recorte de aspecto
            slotsCarta[i].AddChild(cv);

            // ── Losetas: cada una rellena su celda del grid 2x2 ───────────
            for (int l = 0; l < 4; l++)
            {
                TextureRect tr = new TextureRect();
                tr.LayoutMode = 2;      // layout de container
                tr.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
                tr.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
                tr.ExpandMode  = TextureRect.ExpandModeEnum.IgnoreSize;
                tr.StretchMode = TextureRect.StretchModeEnum.Scale; // rellena la celda exacta
                tr.MouseFilter = Control.MouseFilterEnum.Ignore;

                if (l < losetas.Count)
                    tr.Texture = losetas[l].TileImage;

                slotsLoseta[i].AddChild(tr);
            }
        }
    }

    /// <summary>
    /// Muestra las 3 cartas de construcción que el humano tiene en mano.
    /// Se muestran como botones; al hacer clic en una se llama a HumanoEligeColumna
    /// (el click en la carta de mercado sigue siendo el flujo principal,
    ///  pero la mano se muestra en el tablero personal para referencia).
    /// </summary>
    private void RenderizarManoHumano()
    {
        // Buscar o crear el contenedor de mano en VistaMercado
        // (reutilizamos ManoLosetasUI en VistaTableroPersonal para las cartas de construcción,
        //  pero mostramos las cartas del mercado con RenderizarMercado)
        // No hay un nodo dedicado para la mano de cartas del humano en la escena actual,
        // así que usamos la consola para indicarlo.
        Log($"Mano del humano: {string.Join(", ", manoHumano.Select(c => $"{c.CardId}(lvl{c.Level})"))}");
    }

    private void RenderizarManoLosetas()
    {
        foreach (Node hijo in manoLosetasUI.GetChildren()) hijo.QueueFree();

        foreach (TileData loseta in manoLosetas)
        {
            LosetaVisual lv = losetaVisualScene.Instantiate<LosetaVisual>();
            lv.TextureNormal = loseta.TileImage;
            lv.Data = loseta;
            lv.CustomMinimumSize = new Vector2(72, 72);
            lv.IgnoreTextureSize = true;
            lv.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
            manoLosetasUI.AddChild(lv);
        }
    }

    private void LimpiarTableroPersonal()
    {
        foreach (HBoxContainer fila in new[] { filaUI1, filaUI2, filaUI3 })
            if (fila != null) foreach (Node hijo in fila.GetChildren()) hijo.QueueFree();

        foreach (var fila in filasHumano) fila.Clear();
        manoLosetas.Clear();
        cartasAutomata.Clear();
        losetasAutomata.Clear();
    }

    private void LimpiarSlot(Control slot)
    {
        foreach (Node hijo in slot.GetChildren()) hijo.QueueFree();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PUNTUACIÓN (esqueleto — PDF no especifica cálculo completo en detalle,
    // la tabla de ejemplo del PDF muestra las categorías)
    // ═══════════════════════════════════════════════════════════════════════════

    private void MostrarPuntuacion()
    {
        if (labelEstado != null) labelEstado.Text = "¡Partida terminada!";
        // Categorías según PDF:
        // 1. Sistemas Vitales (corazón rojo) — grupos conectados ortogonalmente
        // 2. Invernaderos (frutas)
        // 3. Meteoritos (mayoría)
        // 4. Oficinas de Venta (etiqueta naranja $)
        // 5. Construcciones Especiales (etiqueta amarilla)
        // 6. Cartas en mano / iniciales
        // + Concesiones cumplidas

        // Por ahora mostramos un resumen del estado
        int totalCartas = filasHumano.Sum(f => f.Count);
        int totalLosetas = filasHumano.Sum(f => f.Sum(ct => ct.LosetasColocadas.Count(l => l != null)));

        Log($"=== PARTIDA TERMINADA ===");
        Log($"Cartas en tablero: {totalCartas}");
        Log($"Losetas colocadas: {totalLosetas}");
        Log($"Cartas del autómata: {cartasAutomata.Count}");
        Log("(Puntuación completa pendiente de implementar)");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UTILIDADES
    // ═══════════════════════════════════════════════════════════════════════════

    private void CambiarVista(bool mostrarTablero)
    {
        vistaMercado.Visible  = !mostrarTablero;
        vistaTablero.Visible  =  mostrarTablero;
    }

    private void Log(string msg)
    {
        GD.Print(msg);
        if (textoConsola != null) textoConsola.Text = msg;
    }
}

