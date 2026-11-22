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
    // 4 filas (índices 0-3). Máximo 3 pueden tener cartas al mismo tiempo.
    // La primera carta va siempre a fila 1 (índice 1).
    // Cada fila es un diccionario columnaGlobal → CartaTablero.
    // columnaGlobal es un índice entero que crece hacia la derecha.
    private Dictionary<int, CartaTablero>[] filasHumano = {
        new Dictionary<int, CartaTablero>(),  // fila 0
        new Dictionary<int, CartaTablero>(),  // fila 1
        new Dictionary<int, CartaTablero>(),  // fila 2
        new Dictionary<int, CartaTablero>()   // fila 3
    };

    // Rango de columnas ocupadas en todo el tablero (para renderizado)
    private int colMin = 0, colMax = 0;

    // Losetas en mano del humano (robadas en este turno, listas para colocar)
    private List<TileData> manoLosetas = new List<TileData>();

    // ── Tablero del autómata ─────────────────────────────────────────────────────
    // Solo guardamos las cartas y losetas que tomó (para puntuación futura)
    private List<CardData> cartasAutomata = new List<CardData>();
    private List<TileData> losetasAutomata = new List<TileData>();

    // ── Concesiones (fijas durante toda la partida) ──────────────────────────────
    private bool concesionesFijadas = false;

    // Seguimiento de concesiones cumplidas: fase en que se cumplió (1,2,3) o 0=no cumplida
    // Índices: 0=ConcesionLargo(roja), 1=ConcesionCorto1(verde), 2=ConcesionCorto2(verde)
    private ConcessionData concesionDataLargo, concesionDataCorto1, concesionDataCorto2;
    private int[] faseCumplimientoConcesion = { 0, 0, 0 };

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

    // Tablero personal: cuatro HBoxContainer de filas (Fila0..Fila3)
    private HBoxContainer filaUI0, filaUI1, filaUI2, filaUI3;

    // Panel de posiciones válidas (overlay sobre VistaTableroPersonal)
    // Botones "+" creados dinámicamente al abrir la selección
    private Control overlayPosiciones;

    // Botones
    private Button btnIrTablero, btnVolverMercado;
    private Button btnFase1, btnFase2, btnFase3;
    private Label labelEstado;

    // Carta pendiente de colocación (mientras la selección está abierta)
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

        // 4 filas del tablero personal
        filaUI0 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila0");
        filaUI1 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila1");
        filaUI2 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila2");
        filaUI3 = GetNode<HBoxContainer>("/root/Mundo/InterfazJuego/VistaTableroPersonal/ScrollContainer/VBoxContainer/Fila3");

        // Overlay de posiciones válidas — se crea dinámicamente encima del tablero
        overlayPosiciones = new Control();
        overlayPosiciones.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlayPosiciones.LayoutMode = 1;
        overlayPosiciones.AnchorRight = 1f;
        overlayPosiciones.AnchorBottom = 1f;
        overlayPosiciones.MouseFilter = Control.MouseFilterEnum.Ignore;
        vistaTablero.AddChild(overlayPosiciones);

        // Botones de fase: ocultos — progresión automática
        btnFase1 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase1");
        btnFase2 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase2");
        btnFase3 = GetNode<Button>("/root/Mundo/InterfazJuego/BtnFase3");
        btnFase1.Visible = false;
        btnFase2.Visible = false;
        btnFase3.Visible = false;

        btnIrTablero     = GetNode<Button>("/root/Mundo/InterfazJuego/VistaMercado/BtnIrTablero");
        btnVolverMercado = GetNode<Button>("/root/Mundo/InterfazJuego/VistaTableroPersonal/BtnVolverMercado");

        // "Ver mi Tablero" solo va al tablero si no hay colocación pendiente
        btnIrTablero.Pressed += () => {
            if (cartaPendiente == null) CambiarVista(true);
        };

        // "Volver al mercado" / "Cancelar elección"
        btnVolverMercado.Pressed += () => {
            if (cartaPendiente != null)
            {
                // Cancelar elección de fila — devolver la columna al mercado
                mercado[_colPendiente] = (cartaPendiente, _losetasPendientes);
                cartaPendiente = null;
                _losetasPendientes = null;
                LimpiarOverlay();
                RenderizarTableroPersonal();
                RenderizarMercado();
                btnVolverMercado.Text = "Volver al mercado";
                // Si hay losetas pendientes, quedarse en el tablero para colocarlas
                if (manoLosetas.Count > 0)
                {
                    ActualizarBtnVolver();
                    return;
                }
            }

            // Solo volver al mercado si no hay losetas pendientes
            if (manoLosetas.Count > 0)
            {
                ActualizarBtnVolver();
                return;
            }
            CambiarVista(false);
        };

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

        if (rojas.Count  > 0)
        {
            concesionDataLargo = rojas[rng.Next(rojas.Count)];
            concesionLargo.Texture = concesionDataLargo.ConcessionImage;
        }
        if (verdes.Count > 0)
        {
            concesionDataCorto1 = verdes[rng.Next(verdes.Count)];
            concesionCorto1.Texture = concesionDataCorto1.ConcessionImage;
        }
        if (verdes.Count > 1)
        {
            concesionDataCorto2 = verdes[rng.Next(verdes.Count)];
            concesionCorto2.Texture = concesionDataCorto2.ConcessionImage;
        }
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
        // Si ya hay una colocación pendiente, ignorar
        if (cartaPendiente != null) return;

        if (mercado[colIndex] == null)
        {
            Log("Esa columna ya fue tomada.");
            return;
        }

        var (carta, losetas) = mercado[colIndex].Value;
        mercado[colIndex] = null;

        cartaPendiente = carta;
        _losetasPendientes = losetas;
        _colPendiente = colIndex;

        // Abrir vista tablero y mostrar posiciones válidas como botones "+"
        CambiarVista(true);
        MostrarPosicionesValidas(carta);
    }

    // Variables temporales mientras la colocación está pendiente
    private List<TileData> _losetasPendientes;
    private int _colPendiente;

    // ── Estructura de posición válida ────────────────────────────────────────
    // filaIndex: 0-3
    // alFinal: true = agregar a la derecha, false = agregar a la izquierda
    // esFilaNueva: la fila estaba vacía
    private struct PosicionColocacion
    {
        public int FilaIndex;
        public bool AlFinal;
        public bool EsFilaNueva;
        public int ColGlobal;   // int.MinValue = calcular automáticamente; cualquier otro = columna fija
        public PosicionColocacion(int f, bool final, bool nueva, int col = int.MinValue)
        { FilaIndex = f; AlFinal = final; EsFilaNueva = nueva; ColGlobal = col; }
    }

    /// <summary>
    /// Calcula todas las posiciones donde la carta puede colocarse.
    /// Reglas:
    ///  - Primera carta del tablero → solo fila 1 (índice 1), columna 0.
    ///  - En una fila con cartas: izquierda si carta.Level < mínimo, derecha si ≥ máximo.
    ///  - Nueva fila: solo inmediatamente adyacente a filas ocupadas.
    ///  - Máximo 3 filas activas.
    ///  - Al agregar a izquierda: la nueva columna es colMin-1.
    ///  - Al agregar a derecha:   la nueva columna es colMax+1.
    /// </summary>
    private List<PosicionColocacion> CalcularPosicionesValidas(CardData carta)
    {
        var resultado = new List<PosicionColocacion>();

        int filasOcupadas = 0;
        int primeraFila = -1, ultimaFila = -1;
        for (int f = 0; f < 4; f++)
        {
            if (filasHumano[f].Count > 0)
            {
                filasOcupadas++;
                if (primeraFila < 0) primeraFila = f;
                ultimaFila = f;
            }
        }

        // Caso especial: tablero vacío → primera carta va solo a fila 1
        if (filasOcupadas == 0)
        {
            resultado.Add(new PosicionColocacion(1, true, true));
            return resultado;
        }

        // Posiciones dentro de filas existentes
        for (int f = 0; f < 4; f++)
        {
            var fila = filasHumano[f];
            if (fila.Count == 0) continue;

            int minCol = fila.Keys.Min();
            int maxCol = fila.Keys.Max();
            int nivelIzq = fila[minCol].Data.Level;
            int nivelDer = fila[maxCol].Data.Level;

            // Agregar a la izquierda: columna = minCol - 1 (específica de esta fila)
            if (carta.Level < nivelIzq)
                resultado.Add(new PosicionColocacion(f, false, false, minCol - 1));

            // Agregar a la derecha: columna = maxCol + 1 (específica de esta fila)
            if (carta.Level >= nivelDer)
                resultado.Add(new PosicionColocacion(f, true, false, maxCol + 1));
        }

        // Posiciones de fila nueva (máx 3 filas activas)
        if (filasOcupadas < 3)
        {
            // Fila nueva arriba: encima de la fila más alta
            int arriba = primeraFila - 1;
            if (arriba >= 0 && filasHumano[arriba].Count == 0)
            {
                // "+" en todas las columnas del rango global (no solo las de primeraFila)
                for (int col = colMin; col <= colMax; col++)
                    resultado.Add(new PosicionColocacion(arriba, true, true, col));
            }

            // Fila nueva abajo: debajo de la fila más baja
            int abajo = ultimaFila + 1;
            if (abajo <= 3 && filasHumano[abajo].Count == 0)
            {
                // "+" en todas las columnas del rango global
                for (int col = colMin; col <= colMax; col++)
                    resultado.Add(new PosicionColocacion(abajo, true, true, col));
            }
        }

        return resultado;
    }

    /// <summary>
    /// <summary>
    /// Muestra botones "+" en las posiciones válidas del tablero.
    /// Los botones se insertan DESPUÉS de que RenderizarTableroPersonal
    /// ya colocó los spacers de alineación, para mantener la consistencia visual.
    private void MostrarPosicionesValidas(CardData carta)
    {
        RenderizarTableroPersonal();
        LimpiarOverlay();

        if (btnVolverMercado != null)
            btnVolverMercado.Text = "Cancelar (elegir otra carta)";

        var posiciones = CalcularPosicionesValidas(carta);
        bool tableroVacio = filasHumano.All(f => f.Count == 0);

        if (posiciones.Count == 0)
        {
            Log("Esta carta no cabe en ninguna posición válida. Se descarta.");
            DescartarCartaPendiente();
            return;
        }

        // ── Posiciones en filas existentes (extremo izq / extremo der) ──────
        foreach (var pos in posiciones.FindAll(p => !p.EsFilaNueva || tableroVacio))
        {
            HBoxContainer filaUI = ObtenerFilaUI(pos.FilaIndex);
            var capPos = pos;
            Button btn = new Button();
            btn.Text = "+";
            btn.CustomMinimumSize = new Vector2(44, 170);
            btn.SizeFlagsVertical = Control.SizeFlags.Fill;
            btn.LayoutMode = 2;

            if (tableroVacio || pos.AlFinal)
                filaUI.AddChild(btn);
            else
            {
                filaUI.AddChild(btn);
                filaUI.MoveChild(btn, 0);
            }
            btn.Pressed += () => ConfirmarColocacion(capPos);
        }

        // ── Posiciones en filas nuevas: un "+" por columna del rango ─────────
        // Agrupar por fila nueva y renderizar en una sola pasada
        var filasNuevasPorIdx = new Dictionary<int, List<PosicionColocacion>>();
        foreach (var pos in posiciones.FindAll(p => p.EsFilaNueva && !tableroVacio))
        {
            if (!filasNuevasPorIdx.ContainsKey(pos.FilaIndex))
                filasNuevasPorIdx[pos.FilaIndex] = new List<PosicionColocacion>();
            filasNuevasPorIdx[pos.FilaIndex].Add(pos);
        }

        foreach (var kv in filasNuevasPorIdx)
        {
            HBoxContainer filaUI = ObtenerFilaUI(kv.Key);
            var colsConBoton = new HashSet<int>(kv.Value.ConvertAll(p => p.ColGlobal));

            for (int col = colMin; col <= colMax; col++)
            {
                if (colsConBoton.Contains(col))
                {
                    var posEstaCol = kv.Value.Find(p => p.ColGlobal == col);
                    var capPos = posEstaCol;
                    Button btn = new Button();
                    btn.Text = "+";
                    btn.CustomMinimumSize = new Vector2(120, 170);
                    btn.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
                    btn.SizeFlagsVertical = Control.SizeFlags.Fill;
                    btn.LayoutMode = 2;
                    filaUI.AddChild(btn);
                    btn.Pressed += () => ConfirmarColocacion(capPos);
                }
                else
                {
                    var sp = new Control();
                    sp.CustomMinimumSize = new Vector2(120, 170);
                    sp.MouseFilter = Control.MouseFilterEnum.Ignore;
                    filaUI.AddChild(sp);
                }
            }
        }
    }

    private void ConfirmarColocacion(PosicionColocacion pos)
    {
        if (cartaPendiente == null) return;

        // Limpiar overlay ANTES de agregar la carta (para no confundir "+" con spacers)
        LimpiarOverlay();
        AgregarCartaAFila(cartaPendiente, pos.FilaIndex, pos.AlFinal, pos.ColGlobal);

        foreach (var loseta in _losetasPendientes)
            manoLosetas.Add(loseta);
        RenderizarManoLosetas();

        Log($"Humano colocó carta {cartaPendiente.CardId} (Nv{cartaPendiente.Level}) en fila {pos.FilaIndex + 1}");
        AutomataTomaTurno(_colPendiente);

        cartaPendiente = null;
        _losetasPendientes = null;

        if (btnVolverMercado != null) btnVolverMercado.Text = "Volver al mercado";

        // Solo volver al mercado y avanzar turno si no quedan losetas por colocar
        if (manoLosetas.Count > 0)
        {
            ActualizarBtnVolver();
            return; // el jugador debe colocar todas las losetas primero
        }

        CambiarVista(false);
        RenderizarMercado();
        FinalizarTurno();
    }

    private void DescartarCartaPendiente()
    {
        foreach (var loseta in _losetasPendientes)
            manoLosetas.Add(loseta);
        RenderizarManoLosetas();

        AutomataTomaTurno(_colPendiente);
        cartaPendiente = null;
        _losetasPendientes = null;

        if (btnVolverMercado != null) btnVolverMercado.Text = "Volver al mercado";

        if (manoLosetas.Count > 0)
        {
            ActualizarBtnVolver();
            return;
        }

        CambiarVista(false);
        RenderizarMercado();
        FinalizarTurno();
    }

    /// <summary>
    /// Actualiza el texto del botón según el estado actual.
    /// Si hay losetas pendientes, indica cuántas faltan por colocar.
    /// </summary>
    private void ActualizarBtnVolver()
    {
        if (btnVolverMercado == null) return;
        if (manoLosetas.Count > 0)
            btnVolverMercado.Text = $"Colocar losetas ({manoLosetas.Count} restante(s))";
        else
            btnVolverMercado.Text = "Volver al mercado";
    }

    /// <summary>
    /// Se llama cuando se coloca una loseta en el tablero personal.
    /// Si la mano queda vacía, avanza el turno automáticamente.
    /// </summary>
    public void NotificarLosetaColocada()
    {
        RenderizarManoLosetas();
        ActualizarBtnVolver();

        if (manoLosetas.Count == 0)
        {
            // Todas las losetas colocadas — avanzar turno
            CambiarVista(false);
            RenderizarMercado();
            FinalizarTurno();
        }
    }

    private void LimpiarOverlay()
    {
        if (overlayPosiciones == null) return;
        // Quitar botones "+" que se añadieron a las filas UI
        foreach (HBoxContainer fila in new[] { filaUI0, filaUI1, filaUI2, filaUI3 })
        {
            if (fila == null) continue;
            foreach (Node hijo in fila.GetChildren())
                if (hijo is Button b && b.Text == "+") b.QueueFree();
        }
    }

    private void AgregarCartaAFila(CardData carta, int filaIndex, bool alFinal, int colGlobal = int.MinValue)
    {
        CartaTablero nodo = cartaTableroScene.Instantiate<CartaTablero>();
        nodo.CustomMinimumSize = new Vector2(120, 170);
        nodo.DataPendiente = carta;

        bool tableroVacio = filasHumano.All(f => f.Count == 0);
        int col;
        if (tableroVacio)
        {
            col = 0;
            colMin = 0; colMax = 0;
        }
        else if (colGlobal != int.MinValue)
        {
            // Columna exacta especificada por CalcularPosicionesValidas
            col = colGlobal;
            if (col < colMin) colMin = col;
            if (col > colMax) colMax = col;
        }
        else if (alFinal)
        {
            col = colMax + 1;
            colMax = col;
        }
        else
        {
            col = colMin - 1;
            colMin = col;
        }

        filasHumano[filaIndex][col] = nodo;
        RenderizarTableroPersonal();

        Log($"  → Carta {carta.CardId} (Nv{carta.Level}) en fila {filaIndex + 1}, col {col}");
    }

    /// <summary>
    /// Re-renderiza las 4 filas. Los CartaTablero del diccionario NUNCA se destruyen;
    /// solo los spacers (Control sin script) se recrean en cada render.
    /// </summary>
    private void RenderizarTableroPersonal()
    {
        for (int f = 0; f < 4; f++)
        {
            var filaUI = ObtenerFilaUI(f);
            if (filaUI == null) continue;

            // 1. Sacar todas las CartaTablero del árbol (sin destruirlas)
            foreach (Node hijo in filaUI.GetChildren().ToArray())
            {
                if (hijo is CartaTablero) filaUI.RemoveChild(hijo);
                else if (hijo is not Button) hijo.QueueFree(); // spacers y otros nodos sí se liberan
            }

            if (filasHumano[f].Count == 0) continue;

            // 2. Volver a añadir en orden de columna global, con spacers donde no hay carta
            for (int col = colMin; col <= colMax; col++)
            {
                if (filasHumano[f].TryGetValue(col, out CartaTablero carta))
                {
                    // La CartaTablero ya no tiene padre (fue removida arriba) → AddChild directo
                    filaUI.AddChild(carta);
                }
                else
                {
                    Control spacer = new Control();
                    spacer.CustomMinimumSize = new Vector2(120, 170);
                    spacer.MouseFilter = Control.MouseFilterEnum.Ignore;
                    filaUI.AddChild(spacer);
                }
            }
        }
    }

    private HBoxContainer ObtenerFilaUI(int idx) => idx switch
    {
        0 => filaUI0,
        1 => filaUI1,
        2 => filaUI2,
        3 => filaUI3,
        _ => filaUI1
    };

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
        EvaluarConcesiones(FaseActual);

        if (FaseActual < 3)
        {
            Log("Pasando a la siguiente fase…");
            IniciarFase(FaseActual + 1);
        }
        else
        {
            Log("¡Partida terminada! Calculando puntuación.");
            MostrarPuntuacion();
        }
    }

    /// <summary>
    /// Evalúa si el jugador cumple las concesiones al final de la fase dada.
    /// Cada concesión solo puede marcarse una vez (la primera fase en que se cumple).
    /// La condición de cumplimiento se basa en RequiredPoints de la concesión
    /// comparado con el total de Sistemas Vitales del jugador.
    /// </summary>
    private void EvaluarConcesiones(int fase)
    {
        // Calculamos la cantidad total de Vitales e Invernaderos como métrica de cumplimiento
        // (RequiredPoints indica cuántos elementos de ese tipo necesitas)
        int totalVitales = ContarTiposHumano(TileData.TileType.VitalCubo,
                                             TileData.TileType.VitalTorre,
                                             TileData.TileType.VitalCupula);
        int totalInvernaderos = ContarTiposHumano(TileData.TileType.InvernaderoVerde,
                                                   TileData.TileType.InvernaderoNaranja,
                                                   TileData.TileType.InvernaderoAmarillo);
        int totalLosetas = filasHumano.Sum(f => f.Values.Sum(ct => ct.LosetasColocadas.Count(l => l != null)));

        // Concesión Larga (roja): 100% en fase 1-2, 50% en fase 3
        if (faseCumplimientoConcesion[0] == 0 && concesionDataLargo != null)
        {
            if (ConcesionCumplida(concesionDataLargo, totalVitales, totalInvernaderos, totalLosetas))
                faseCumplimientoConcesion[0] = fase;
        }
        // Concesiones Cortas (verdes): 100% en fase 1, 50% en fase 2, 0 en fase 3
        if (faseCumplimientoConcesion[1] == 0 && concesionDataCorto1 != null)
        {
            if (ConcesionCumplida(concesionDataCorto1, totalVitales, totalInvernaderos, totalLosetas))
                faseCumplimientoConcesion[1] = fase;
        }
        if (faseCumplimientoConcesion[2] == 0 && concesionDataCorto2 != null)
        {
            if (ConcesionCumplida(concesionDataCorto2, totalVitales, totalInvernaderos, totalLosetas))
                faseCumplimientoConcesion[2] = fase;
        }
    }

    private bool ConcesionCumplida(ConcessionData c, int vitales, int invernaderos, int totalLosetas)
        => totalLosetas >= c.RequiredPoints; // RequiredPoints = mínimo de losetas colocadas requeridas

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
                // Colocada: destruir fantasma, actualizar mano y avanzar turno si mano vacía
                manoLosetas.Remove(loseta);
                fantasma.QueueFree();
                NotificarLosetaColocada();
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
        for (int f = 0; f < 4; f++)
        {
            foreach (CartaTablero ct in filasHumano[f].Values)
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

            // Carta — clickeable solo si tiene posiciones válidas en el tablero
            CardVisual cv = cartaVisualScene.Instantiate<CardVisual>();
            cv.TextureNormal = carta.CardImage;
            cv.Data = carta;
            cv.CardIndex = i;
            cv.LayoutMode = 1;
            cv.AnchorLeft   = 0f;
            cv.AnchorTop    = 0f;
            cv.AnchorRight  = 1f;
            cv.AnchorBottom = 1f;
            cv.OffsetLeft   = 0f;
            cv.OffsetTop    = 0f;
            cv.OffsetRight  = 0f;
            cv.OffsetBottom = 0f;
            cv.IgnoreTextureSize = true;
            cv.StretchMode = TextureButton.StretchModeEnum.Scale;

            // Si no hay posición válida para esta carta, desactivarla visualmente
            bool tieneEspacio = CalcularPosicionesValidas(carta).Count > 0;
            if (!tieneEspacio)
            {
                cv.Disabled = true;
                cv.Modulate = new Color(1f, 1f, 1f, 0.35f); // semitransparente = no disponible
                cv.TooltipText = "No hay espacio en el tablero para esta carta";
            }

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

        // Si todas las cartas disponibles están bloqueadas (ninguna cabe en el tablero),
        // el jugador no puede elegir ninguna — avanzar turno automáticamente descartando todo.
        bool todasBloqueadas = true;
        for (int i = 0; i < 3; i++)
        {
            if (mercado[i] == null) continue;
            if (CalcularPosicionesValidas(mercado[i].Value.Carta).Count > 0)
            {
                todasBloqueadas = false;
                break;
            }
        }
        if (todasBloqueadas && mercado.Any(m => m != null))
        {
            Log("Ninguna carta del mercado cabe en el tablero — turno forzado a avanzar.");
            textoConsola.Text = "Ninguna carta disponible cabe en tu tablero. Turno omitido.";
            // Dejar que el jugador vea el mensaje un frame, luego avanzar
            CallDeferred(MethodName.ForzarAvanceTurnoSinCarta);
        }
    }

    /// <summary>
    /// Se llama cuando ninguna carta del mercado tiene espacio en el tablero.
    /// El autómata toma una columna aleatoria y el turno avanza.
    /// </summary>
    private void ForzarAvanceTurnoSinCarta()
    {
        // El autómata toma la primera columna disponible
        for (int col = 0; col < 3; col++)
        {
            if (mercado[col] != null)
            {
                AutomataTomaTurno(col);
                // Descartar las losetas de esa columna (van al jugador igual, como compensación)
                // Según el PDF no hay indicación — simplemente se descarta la columna del humano
                mercado[col] = null;
                break;
            }
        }
        RenderizarMercado();
        FinalizarTurno();
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
        foreach (HBoxContainer fila in new[] { filaUI0, filaUI1, filaUI2, filaUI3 })
            if (fila != null) foreach (Node hijo in fila.GetChildren()) hijo.QueueFree();

        foreach (var fila in filasHumano) fila.Clear();
        colMin = 0; colMax = 0;
        manoLosetas.Clear();
        cartasAutomata.Clear();
        losetasAutomata.Clear();
        LimpiarOverlay();
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

        var r = new ResultadoPartida();

        // ═══════════════════════════════════════════════════
        // HUMANO
        // ═══════════════════════════════════════════════════

        // 1. Sistemas Vitales — grupos conectados ortogonalmente
        r.HumanoVitales = CalcularPuntosGrupos(EsTipoVital);

        // 2. Invernaderos — grupos + tríos de 3 frutas distintas
        r.HumanoInvernaderos  = CalcularPuntosGrupos(EsTipoInvernadero);
        r.HumanoInvernaderos += CalcularTriosInvernadero();

        // 3. Meteoritos (mayoría)
        int meteorosHumano   = ContarTiposHumano(TileData.TileType.Meteorito);
        int meteorosAutomata = losetasAutomata.Count(l => l.Type == TileData.TileType.Meteorito);
        // también contar meteoritos impresos en cartas del humano
        meteorosHumano   += ContarMeteoritosImpresosHumano();
        meteorosAutomata += ContarMeteoritosImpresosAutomata();
        r.HumanoMeteorita  = meteorosHumano > meteorosAutomata ? 10 : 0;

        // 4. Oficinas de Venta
        r.HumanoOficinas = ContarTiposHumano(TileData.TileType.Oficina) * 2;

        // 5. Construcciones Especiales (efecto espacial simplificado → valor fijo 3 pts por pieza)
        int especiales = ContarTiposHumano(TileData.TileType.EspecialModulo,
                                           TileData.TileType.EspecialComplejo,
                                           TileData.TileType.EspecialRover);
        r.HumanoEspeciales = especiales * 3;

        // 6. Cartas iniciales conservadas (en este flujo siempre se juegan, pero las 3 dan 9 pts)
        r.HumanoCartasIniciales = 9;

        // 7. Concesiones del humano
        r.HumanoConcesiones = CalcularPuntosConcesionesHumano();

        // ═══════════════════════════════════════════════════
        // AUTÓMATA
        // ═══════════════════════════════════════════════════

        // 1. Sistemas Vitales — acumulativo (todos como si conectados)
        int autoVitalTotal = losetasAutomata.Count(l => EsTipoVital(l.Type));
        autoVitalTotal += ContarVitalesImpresosAutomata();
        r.AutomataVitales = PuntosGrupoPorTamano(autoVitalTotal);

        // 2. Invernaderos — acumulativo + tríos
        int autoInvTotal = losetasAutomata.Count(l => EsTipoInvernadero(l.Type));
        r.AutomataInvernaderos  = PuntosGrupoPorTamano(autoInvTotal);
        r.AutomataInvernaderos += CalcularTriosAutomata();

        // 3. Meteoritos (mayoría)
        r.AutomataMeteorita = meteorosAutomata > meteorosHumano ? 10 : 0;

        // 4. Oficinas
        r.AutomataOficinas = losetasAutomata.Count(l => l.Type == TileData.TileType.Oficina) * 2;

        // 5. Construcciones Especiales — 5 pts fijos cada una
        r.AutomataEspeciales = losetasAutomata.Count(l =>
            l.Type == TileData.TileType.EspecialModulo ||
            l.Type == TileData.TileType.EspecialComplejo ||
            l.Type == TileData.TileType.EspecialRover) * 5;

        // 6. Losetas de Acción — 2 pts cada una
        r.AutomataAccion = losetasAutomata.Count(l => l.Type == TileData.TileType.Accion) * 2;

        // 7. Cartas iniciales — bono fijo
        r.AutomataCartasIniciales = 9;

        // 8. Concesiones del autómata — siempre cumple todas al 100%
        r.AutomataConcesiones = 0;
        if (concesionDataLargo  != null) r.AutomataConcesiones += concesionDataLargo.VictoryPoints;
        if (concesionDataCorto1 != null) r.AutomataConcesiones += concesionDataCorto1.VictoryPoints;
        if (concesionDataCorto2 != null) r.AutomataConcesiones += concesionDataCorto2.VictoryPoints;

        // ═══════════════════════════════════════════════════
        // TOTALES Y RESULTADO
        // ═══════════════════════════════════════════════════
        r.TotalHumano   = r.HumanoVitales + r.HumanoInvernaderos + r.HumanoMeteorita +
                          r.HumanoOficinas + r.HumanoEspeciales + r.HumanoCartasIniciales +
                          r.HumanoConcesiones;
        r.TotalAutomata = r.AutomataVitales + r.AutomataInvernaderos + r.AutomataMeteorita +
                          r.AutomataOficinas + r.AutomataEspeciales + r.AutomataAccion +
                          r.AutomataCartasIniciales + r.AutomataConcesiones;
        // Empate → gana el autómata
        r.GanaHumano = r.TotalHumano > r.TotalAutomata;

        MostrarVistaResultados(r);
    }

    // ── Estructura de resultado ───────────────────────────────────────────────
    private struct ResultadoPartida
    {
        public int HumanoVitales, HumanoInvernaderos, HumanoMeteorita, HumanoOficinas,
                   HumanoEspeciales, HumanoCartasIniciales, HumanoConcesiones;
        public int AutomataVitales, AutomataInvernaderos, AutomataMeteorita, AutomataOficinas,
                   AutomataEspeciales, AutomataAccion, AutomataCartasIniciales, AutomataConcesiones;
        public int TotalHumano, TotalAutomata;
        public bool GanaHumano;
    }

    // ── Helpers de cálculo ────────────────────────────────────────────────────

    private static bool EsTipoVital(TileData.TileType t) =>
        t == TileData.TileType.VitalCubo ||
        t == TileData.TileType.VitalTorre ||
        t == TileData.TileType.VitalCupula;

    private static bool EsTipoInvernadero(TileData.TileType t) =>
        t == TileData.TileType.InvernaderoVerde ||
        t == TileData.TileType.InvernaderoNaranja ||
        t == TileData.TileType.InvernaderoAmarillo;

    // Tabla de puntos por tamaño de grupo (1=1, 2=3, 3=6, 4=10, N≥5=N*2.5≈)
    private static int PuntosGrupoPorTamano(int tam)
    {
        if (tam <= 0) return 0;
        if (tam == 1) return 1;
        if (tam == 2) return 3;
        if (tam == 3) return 6;
        if (tam == 4) return 10;
        return 10 + (tam - 4) * 3; // grupos grandes: +3 por cada elemento extra
    }

    /// <summary>
    /// Flood-fill sobre el tablero 2D del humano.
    /// Cada "celda" es (filaIndex, colGlobal, slotIndex[0-3]).
    /// Dos celdas son adyacentes si comparten un borde: mismo slot en columna vecina,
    /// o slots adyacentes dentro de la misma carta (sup-izq↔sup-der, inf-izq↔inf-der,
    /// sup-izq↔inf-izq, sup-der↔inf-der).
    /// </summary>
    private int CalcularPuntosGrupos(System.Func<TileData.TileType, bool> esTipo)
    {
        // Construir mapa de celdas con losetas del tipo indicado
        // Incluir también elementos impresos en cartas que coincidan con el tipo
        var celdas = new HashSet<(int f, int col, int slot)>();
        for (int f = 0; f < 4; f++)
        {
            foreach (var kv in filasHumano[f])
            {
                int col = kv.Key;
                CartaTablero ct = kv.Value;
                for (int s = 0; s < 4; s++)
                {
                    // Loseta colocada
                    if (ct.LosetasColocadas[s] != null && esTipo(ct.LosetasColocadas[s].Type))
                        celdas.Add((f, col, s));
                    // Elemento impreso en la carta
                    else if (EsElementoImpresoDeTipo(ct.Data, s, esTipo))
                        celdas.Add((f, col, s));
                }
            }
        }

        var visitadas = new HashSet<(int, int, int)>();
        int puntosTotales = 0;

        foreach (var celda in celdas)
        {
            if (visitadas.Contains(celda)) continue;
            // BFS para encontrar el grupo conectado
            int tamGrupo = 0;
            var cola = new Queue<(int f, int col, int slot)>();
            cola.Enqueue(celda);
            visitadas.Add(celda);
            while (cola.Count > 0)
            {
                var (f, col, slot) = cola.Dequeue();
                tamGrupo++;
                foreach (var vec in ObtenerVecinos(f, col, slot))
                {
                    if (celdas.Contains(vec) && !visitadas.Contains(vec))
                    {
                        visitadas.Add(vec);
                        cola.Enqueue(vec);
                    }
                }
            }
            puntosTotales += PuntosGrupoPorTamano(tamGrupo);
        }
        return puntosTotales;
    }

    // Devuelve todas las celdas ortogonalmente adyacentes a (f, col, slot)
    private IEnumerable<(int, int, int)> ObtenerVecinos(int f, int col, int slot)
    {
        // Vecinos dentro de la misma carta:
        // slot 0=SupIzq, 1=SupDer, 2=InfIzq, 3=InfDer
        // SupIzq ↔ SupDer (misma fila superior)
        // InfIzq ↔ InfDer (misma fila inferior)
        // SupIzq ↔ InfIzq (misma columna izquierda)
        // SupDer ↔ InfDer (misma columna derecha)
        int[] mismaCartaVecinos = slot switch {
            0 => new[]{1, 2},
            1 => new[]{0, 3},
            2 => new[]{0, 3},
            3 => new[]{1, 2},
            _ => System.Array.Empty<int>()
        };
        foreach (int s2 in mismaCartaVecinos)
            yield return (f, col, s2);

        // Vecinos en carta de la misma fila, columna adyacente
        // slot 0 (SupIzq) ↔ slot 1 (SupDer) de la carta a la izquierda
        // slot 1 (SupDer) ↔ slot 0 (SupIzq) de la carta a la derecha
        // slot 2 (InfIzq) ↔ slot 3 (InfDer) de la carta a la izquierda
        // slot 3 (InfDer) ↔ slot 2 (InfIzq) de la carta a la derecha
        (int colVec, int slotVec)[] vecColum = slot switch {
            0 => new[]{(col-1, 1)},
            1 => new[]{(col+1, 0)},
            2 => new[]{(col-1, 3)},
            3 => new[]{(col+1, 2)},
            _ => System.Array.Empty<(int,int)>()
        };
        foreach (var (cv, sv) in vecColum)
            yield return (f, cv, sv);

        // Vecinos en la fila adyacente (arriba/abajo), misma columna
        // slot SupIzq(0)/SupDer(1) de fila f ↔ slot InfIzq(2)/InfDer(3) de fila f-1
        // slot InfIzq(2)/InfDer(3) de fila f ↔ slot SupIzq(0)/SupDer(1) de fila f+1
        (int filaVec, int slotVecF)[] vecFila = slot switch {
            0 => new[]{(f-1, 2)},
            1 => new[]{(f-1, 3)},
            2 => new[]{(f+1, 0)},
            3 => new[]{(f+1, 1)},
            _ => System.Array.Empty<(int,int)>()
        };
        foreach (var (fv, sv) in vecFila)
            if (fv >= 0 && fv < 4) yield return (fv, col, sv);
    }

    private bool EsElementoImpresoDeTipo(CardData card, int slot, System.Func<TileData.TileType, bool> esTipo)
    {
        if (card == null) return false;
        string[] nombres = { "Sup-Izq", "Sup-Der", "Inf-Izq", "Inf-Der" };
        foreach (var e in card.Spaces)
        {
            if (e.Position != nombres[slot]) continue;
            if (!e.IsOccupied) continue;
            // Mapear tipo de elemento impreso a TileType aproximado
            var tipoAprox = MapearContenidoATipo(e.Content);
            return tipoAprox.HasValue && esTipo(tipoAprox.Value);
        }
        return false;
    }

    private static TileData.TileType? MapearContenidoATipo(string content) => content switch {
        "Edif.Azul" or "Agua"       => TileData.TileType.VitalTorre,
        "Hábitat"                   => TileData.TileType.VitalCubo,
        "Invernadero"               => TileData.TileType.InvernaderoVerde,
        "Cráter" or "Astro"         => TileData.TileType.Meteorito,
        "Satélite"                  => TileData.TileType.Oficina,
        _                           => (TileData.TileType?)null
    };

    private int ContarTiposHumano(params TileData.TileType[] tipos)
    {
        int count = 0;
        foreach (var fila in filasHumano)
            foreach (var ct in fila.Values)
                foreach (var l in ct.LosetasColocadas)
                    if (l != null && tipos.Contains(l.Type)) count++;
        return count;
    }

    private int ContarMeteoritosImpresosHumano()
    {
        int c = 0;
        foreach (var fila in filasHumano)
            foreach (var ct in fila.Values)
                if (ct.Data != null)
                    foreach (var e in ct.Data.Spaces)
                        if (e.IsOccupied && (e.Content == "Astro" || e.Content == "Cráter")) c++;
        return c;
    }

    private int ContarMeteoritosImpresosAutomata() =>
        cartasAutomata.Sum(c => c.Spaces.Count(e => e.IsOccupied && (e.Content == "Astro" || e.Content == "Cráter")));

    private int ContarVitalesImpresosAutomata() =>
        cartasAutomata.Sum(c => c.Spaces.Count(e => e.IsOccupied &&
            (e.Content == "Edif.Azul" || e.Content == "Agua" || e.Content == "Hábitat")));

    private int CalcularTriosInvernadero()
    {
        int v = 0, n = 0, am = 0;
        foreach (var fila in filasHumano)
            foreach (var ct in fila.Values)
                foreach (var l in ct.LosetasColocadas)
                {
                    if (l == null) continue;
                    if (l.Type == TileData.TileType.InvernaderoVerde)   v++;
                    if (l.Type == TileData.TileType.InvernaderoNaranja) n++;
                    if (l.Type == TileData.TileType.InvernaderoAmarillo) am++;
                }
        return System.Math.Min(System.Math.Min(v, n), am) * 5;
    }

    private int CalcularTriosAutomata()
    {
        int v = losetasAutomata.Count(l => l.Type == TileData.TileType.InvernaderoVerde);
        int n = losetasAutomata.Count(l => l.Type == TileData.TileType.InvernaderoNaranja);
        int am = losetasAutomata.Count(l => l.Type == TileData.TileType.InvernaderoAmarillo);
        return System.Math.Min(System.Math.Min(v, n), am) * 5;
    }

    private int CalcularPuntosConcesionesHumano()
    {
        int pts = 0;
        // Concesión roja: 100% en fase 1-2, 50% en fase 3
        if (faseCumplimientoConcesion[0] > 0 && concesionDataLargo != null)
        {
            int base_ = concesionDataLargo.VictoryPoints;
            pts += faseCumplimientoConcesion[0] < 3 ? base_ : (int)System.Math.Ceiling(base_ / 2.0);
        }
        // Concesiones verdes: 100% en fase 1, 50% en fase 2, 0 en fase 3
        if (faseCumplimientoConcesion[1] > 0 && concesionDataCorto1 != null)
        {
            int base_ = concesionDataCorto1.VictoryPoints;
            if (faseCumplimientoConcesion[1] == 1) pts += base_;
            else if (faseCumplimientoConcesion[1] == 2) pts += (int)System.Math.Ceiling(base_ / 2.0);
        }
        if (faseCumplimientoConcesion[2] > 0 && concesionDataCorto2 != null)
        {
            int base_ = concesionDataCorto2.VictoryPoints;
            if (faseCumplimientoConcesion[2] == 1) pts += base_;
            else if (faseCumplimientoConcesion[2] == 2) pts += (int)System.Math.Ceiling(base_ / 2.0);
        }
        return pts;
    }

    private void MostrarVistaResultados(ResultadoPartida r)
    {
        // Ocultar todo lo demás y mostrar el panel de resultados
        vistaMercado.Visible = false;
        vistaTablero.Visible = false;

        // Crear panel de resultados dinámicamente si no existe
        var existente = GetTree().Root.FindChild("PantallaResultados", true, false);
        existente?.QueueFree();

        // Panel de fondo semiopaco que cubre toda la pantalla
        var overlay = new ColorRect();
        overlay.Name = "PantallaResultados";
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.LayoutMode = 1;
        overlay.AnchorRight = 1f; overlay.AnchorBottom = 1f;
        overlay.Color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // Contenedor central
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
        vbox.LayoutMode = 1;
        vbox.AnchorLeft = 0.1f; vbox.AnchorTop = 0.05f;
        vbox.AnchorRight = 0.9f; vbox.AnchorBottom = 0.95f;
        vbox.GrowHorizontal = Control.GrowDirection.Both;
        vbox.GrowVertical = Control.GrowDirection.Both;
        vbox.AddThemeConstantOverride("separation", 6);

        // Título
        string titulo = r.GanaHumano ? "🎉 ¡GANASTE!" : "El Autómata gana";
        var lblTitulo = new Label();
        lblTitulo.Text = titulo;
        lblTitulo.HorizontalAlignment = HorizontalAlignment.Center;
        lblTitulo.AddThemeFontSizeOverride("font_size", 32);
        lblTitulo.AddThemeColorOverride("font_color",
            r.GanaHumano ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f));
        vbox.AddChild(lblTitulo);

        // Tabla de puntajes como RichTextLabel
        var rtl = new RichTextLabel();
        rtl.SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        rtl.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        rtl.BbcodeEnabled = true;
        rtl.FitContent = false;
        rtl.AddThemeFontSizeOverride("normal_font_size", 18);

        string ganadoH = r.TotalHumano > r.TotalAutomata ? " ✓" : "";
        string ganadoA = r.TotalAutomata >= r.TotalHumano ? " ✓" : "";

        rtl.Text =
            "[center][b]PUNTAJE FINAL[/b][/center]\n\n" +
            $"[table=3]" +
            $"[cell][b]Categoría[/b][/cell][cell][b]  Tú[/b][/cell][cell][b]  Autómata[/b][/cell]" +
            $"[cell]Sistemas Vitales[/cell][cell]  {r.HumanoVitales}[/cell][cell]  {r.AutomataVitales}[/cell]" +
            $"[cell]Invernaderos[/cell][cell]  {r.HumanoInvernaderos}[/cell][cell]  {r.AutomataInvernaderos}[/cell]" +
            $"[cell]Meteoritos[/cell][cell]  {r.HumanoMeteorita}[/cell][cell]  {r.AutomataMeteorita}[/cell]" +
            $"[cell]Oficinas de Venta[/cell][cell]  {r.HumanoOficinas}[/cell][cell]  {r.AutomataOficinas}[/cell]" +
            $"[cell]Constr. Especiales[/cell][cell]  {r.HumanoEspeciales}[/cell][cell]  {r.AutomataEspeciales}[/cell]" +
            $"[cell]Losetas de Acción[/cell][cell]  —[/cell][cell]  {r.AutomataAccion}[/cell]" +
            $"[cell]Cartas iniciales[/cell][cell]  {r.HumanoCartasIniciales}[/cell][cell]  {r.AutomataCartasIniciales}[/cell]" +
            $"[cell]Concesiones[/cell][cell]  {r.HumanoConcesiones}[/cell][cell]  {r.AutomataConcesiones}[/cell]" +
            $"[cell][b]TOTAL[/b][/cell]" +
            $"[cell][b]  {r.TotalHumano}{ganadoH}[/b][/cell]" +
            $"[cell][b]  {r.TotalAutomata}{ganadoA}[/b][/cell]" +
            $"[/table]";

        vbox.AddChild(rtl);

        overlay.AddChild(vbox);

        // Añadir a la capa de UI por encima de todo
        var interfaz = GetNode<CanvasLayer>("/root/Mundo/InterfazJuego");
        interfaz.AddChild(overlay);

        // Imprimir también en log
        GD.Print($"=== PUNTAJE FINAL === Humano:{r.TotalHumano} Autómata:{r.TotalAutomata}");
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

