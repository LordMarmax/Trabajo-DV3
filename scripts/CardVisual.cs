using Godot;

public partial class CardVisual : TextureButton
{
    public int CardIndex = 0;
    public CardData Data;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            var gm = GetNode<GameManager>("/root/Mundo/LogicaJuego");
            if (gm.ItemArrastrado != null) return;
            gm.IniciarArrastre(this, GetParent<Control>(), GlobalPosition, Data, mouseEvent.Position);
            AcceptEvent();
        }
    }
}