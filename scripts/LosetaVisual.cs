using Godot;

/// <summary>
/// Representa una loseta visual.
/// Solo permite drag-and-drop cuando está dentro de VistaTableroPersonal
/// (es decir, en la mano del jugador). Las losetas del mercado (SlotLoseta)
/// no son interactivas — su mouse_filter en la escena ya es IGNORE (2),
/// así que este script solo actúa cuando el nodo vive bajo VistaTableroPersonal.
/// </summary>
public partial class LosetaVisual : TextureButton
{
    public int TileIndex = 0;
    public TileData Data;

    public override void _GuiInput(InputEvent @event)
    {
        // Solo arrastrar si estamos dentro de la vista del tablero personal
        if (!EstaEnTableroPersonal()) return;

        if (@event is InputEventMouseButton mouseEvent
            && mouseEvent.ButtonIndex == MouseButton.Left
            && mouseEvent.Pressed)
        {
            var gm = GetNode<GameManager>("/root/Mundo/LogicaJuego");
            if (gm.ItemArrastrado != null) return;
            gm.IniciarArrastre(this, GetParent<Control>(), GlobalPosition, Data, mouseEvent.Position);
            AcceptEvent();
        }
    }

    private bool EstaEnTableroPersonal()
    {
        // Recorre la jerarquía de padres buscando "VistaTableroPersonal"
        Node nodo = GetParent();
        while (nodo != null)
        {
            if (nodo.Name == "VistaTableroPersonal") return true;
            nodo = nodo.GetParent();
        }
        return false;
    }
}
