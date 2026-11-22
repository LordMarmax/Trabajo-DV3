using Godot;
using System.Collections.Generic;

/// <summary>
/// Controla una carta del tablero personal del humano.
/// Grid 2x2 de slots:  [0]=Sup-Izq  [1]=Sup-Der
///                     [2]=Inf-Izq  [3]=Inf-Der
///
/// Regla 2x1:
///   Si la carta declara un espacio con forma "2x1V" (vertical) o "2x1H" (horizontal),
///   los dos slots que cubre son un par vinculado: en cuanto se coloca una loseta en
///   cualquiera de ellos, el otro queda bloqueado permanentemente.
/// </summary>
public partial class CartaTablero : Control
{
    public CardData Data { get; private set; }
    // Si se asigna antes de entrar al árbol, _Ready lo usará para inicializar
    public CardData DataPendiente = null;

    private TextureRect fondoCarta;
    // slots[0]=Sup-Izq, [1]=Sup-Der, [2]=Inf-Izq, [3]=Inf-Der
    private Control[] slots = new Control[4];

    public TileData[] LosetasColocadas { get; } = new TileData[4];

    // Índices de pares vinculados por espacios 2x1 (bloqueados entre sí)
    // Cada entrada es (slotA, slotB): colocar en A bloquea B y viceversa
    private List<(int A, int B)> pares2x1 = new List<(int, int)>();

    // Slots bloqueados manualmente (pareja de un 2x1 ya usado)
    private bool[] bloqueadoPor2x1 = new bool[4];

    public override void _Ready()
    {
        fondoCarta = GetNode<TextureRect>("FondoCarta");
        slots[0] = GetNode<Control>("HuecosLosetas/Slot1");
        slots[1] = GetNode<Control>("HuecosLosetas/Slot2");
        slots[2] = GetNode<Control>("HuecosLosetas/Slot3");
        slots[3] = GetNode<Control>("HuecosLosetas/Slot4");

        // Si se asignó DataPendiente antes de entrar al árbol, inicializar ahora
        if (DataPendiente != null)
        {
            Inicializar(DataPendiente);
            DataPendiente = null;
        }
    }

    public void Inicializar(CardData carta)
    {
        Data = carta;
        fondoCarta.Texture = carta.CardImage;
        pares2x1.Clear();
        for (int i = 0; i < 4; i++) bloqueadoPor2x1[i] = false;

        foreach (Node hijo in slots[0].GetChildren()) hijo.QueueFree();
        foreach (Node hijo in slots[1].GetChildren()) hijo.QueueFree();
        foreach (Node hijo in slots[2].GetChildren()) hijo.QueueFree();
        foreach (Node hijo in slots[3].GetChildren()) hijo.QueueFree();

        for (int i = 0; i < 4; i++) slots[i].Modulate = Colors.White;

        foreach (var espacio in carta.Spaces)
        {
            if (espacio.IsOccupied)
            {
                int idx = PosicionAIndice(espacio.Position);
                if (idx >= 0)
                    slots[idx].Modulate = new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            // Registrar pares 2x1: la posición puede ser una región ("Der","Izq","Sup","Inf")
            if (!espacio.IsOccupied && (espacio.Shape == "2x1V" || espacio.Shape == "2x1H"))
            {
                (int slotA, int slotB) = Region2x1ASlots(espacio.Shape, espacio.Position);
                if (slotA >= 0 && slotB >= 0)
                    pares2x1.Add((slotA, slotB));
            }
        }
    }

    /// <summary>
    /// Convierte una región 2x1 a los dos índices de slot que cubre.
    /// 2x1V: columna completa  — "Der"→(1,3)  "Izq"→(0,2)
    /// 2x1H: fila completa     — "Sup"→(0,1)  "Inf"→(2,3)
    /// </summary>
    private static (int, int) Region2x1ASlots(string forma, string region)
    {
        if (forma == "2x1V")
        {
            if (region == "Der") return (1, 3);
            if (region == "Izq") return (0, 2);
        }
        else // 2x1H
        {
            if (region == "Sup") return (0, 1);
            if (region == "Inf") return (2, 3);
        }
        return (-1, -1);
    }

    /// <summary>
    /// Coloca una loseta en el slot indicado. Devuelve true si tuvo éxito.
    /// Si el slot pertenece a un par 2x1, la imagen se pinta centrada
    /// sobre el área combinada de ambos slots y el compañero queda bloqueado.
    /// </summary>
    public bool ColocarLoseta(TileData loseta, int slotIndex)
    {
        if (!SlotAceptaLoseta(slotIndex)) return false;

        LosetasColocadas[slotIndex] = loseta;

        // ¿Pertenece este slot a un par 2x1?
        int pareja = -1;
        foreach (var (a, b) in pares2x1)
        {
            if (a == slotIndex) { pareja = b; break; }
            if (b == slotIndex) { pareja = a; break; }
        }

        if (pareja >= 0)
        {
            // Colocar la imagen centrada sobre la unión de los dos slots.
            // Los rects ya están resueltos porque la carta lleva al menos un frame
            // visible en el tablero antes de que el jugador pueda arrastrar una loseta.
            ColocarImagenCentrada2x1(slotIndex, pareja, loseta);

            // Bloquear el slot compañero
            bloqueadoPor2x1[pareja] = true;
        }
        else
        {
            // Slot normal 1x1
            TextureRect img = new TextureRect();
            img.Texture = loseta.TileImage;
            img.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            img.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            img.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            img.LayoutMode = 1;
            img.AnchorRight  = 1f;
            img.AnchorBottom = 1f;
            img.MouseFilter  = MouseFilterEnum.Ignore;
            slots[slotIndex].AddChild(img);
        }

        return true;
    }

    /// <summary>
    /// Crea un TextureRect posicionado sobre la unión de los dos slots del par 2x1.
    /// Se llama con CallDeferred para que el layout ya haya calculado los rects.
    /// </summary>
    private void ColocarImagenCentrada2x1(int slotA, int slotB, TileData loseta)
    {
        if (!IsInstanceValid(slots[slotA]) || !IsInstanceValid(slots[slotB])) return;

        // Rects de ambos slots en coordenadas locales de CartaTablero
        Rect2 rA = slots[slotA].GetRect();
        Rect2 rB = slots[slotB].GetRect();

        // Unión de los dos rects
        Rect2 union = rA.Merge(rB);

        TextureRect img = new TextureRect();
        img.Texture = loseta.TileImage;
        img.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        img.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        img.MouseFilter = MouseFilterEnum.Ignore;

        // Posición absoluta dentro de CartaTablero (layout_mode 0)
        img.LayoutMode = 0;
        img.Position = union.Position;
        img.Size     = union.Size;

        // Añadir directamente a CartaTablero (encima del GridContainer)
        AddChild(img);
    }

    public bool SlotAceptaLoseta(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 3) return false;
        if (LosetasColocadas[slotIndex] != null) return false;   // ya tiene loseta
        if (bloqueadoPor2x1[slotIndex]) return false;            // bloqueado por pareja 2x1

        CardSpace espacio = ObtenerEspacio(slotIndex);
        if (espacio == null) return true;        // sin descripción = libre
        if (espacio.IsOccupied) return false;    // impreso, no acepta
        return true;
    }

    public Control ObtenerSlotControl(int slotIndex) => slots[slotIndex];

    // Convierte "Sup-Izq"/"Sup-Der"/"Inf-Izq"/"Inf-Der" → 0/1/2/3
    public static int PosicionAIndice(string pos) => pos switch
    {
        "Sup-Izq" => 0,
        "Sup-Der" => 1,
        "Inf-Izq" => 2,
        "Inf-Der" => 3,
        _ => -1
    };

    // ── Helpers privados ─────────────────────────────────────────────────────

    private void MarcarBloqueado(int idx)
    {
        // Color rojizo semitransparente para indicar que está bloqueado
        slots[idx].Modulate = new Color(0.8f, 0.3f, 0.3f, 0.7f);
    }

    private CardSpace ObtenerEspacio(int idx)
    {
        if (Data == null) return null;
        string[] nombres = { "Sup-Izq", "Sup-Der", "Inf-Izq", "Inf-Der" };
        foreach (var e in Data.Spaces)
            if (e.Position == nombres[idx]) return e;
        return null;
    }
}
