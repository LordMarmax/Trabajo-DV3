using Godot;

public partial class CardVisual : TextureButton
{
    public int CardIndex = 0;
    public CardData Data;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            // Si el padre es un CanvasLayer, significa que se quedó atascada en el limbo
            if (GetParent() is CanvasLayer) return; 

            var gm = GetNode<GameManager>("/root/Mundo/LogicaJuego");
            if (gm.ItemArrastrado != null) return; // Evitar arrastrar más de una a la vez

            gm.IniciarArrastre(this, GetParent<Control>(), GlobalPosition, Data, mouseEvent.Position);
            AcceptEvent();
        }
    }
}