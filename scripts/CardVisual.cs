using Godot;

/// <summary>
/// Representa una carta del mercado.
/// Un clic simple selecciona la columna (HumanoEligeColumna).
/// No hay drag — las cartas del mercado solo se toman con click.
/// </summary>
public partial class CardVisual : TextureButton
{
    public int CardIndex = 0;   // columna del mercado (0, 1 o 2)
    public CardData Data;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb
            && mb.ButtonIndex == MouseButton.Left
            && mb.Pressed)
        {
            var gm = GetNode<GameManager>("/root/Mundo/LogicaJuego");
            gm.HumanoEligeColumna(CardIndex);
            AcceptEvent();
        }
    }
}
