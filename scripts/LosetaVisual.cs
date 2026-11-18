using Godot;

public partial class LosetaVisual : TextureButton
{
    public int TileIndex = 0;
    public TileData Data;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            if (GetParent() is CanvasLayer) return;

            var gm = GetNode<GameManager>("/root/Mundo/LogicaJuego");
            if (gm.ItemArrastrado != null) return;

            gm.IniciarArrastre(this, GetParent<Control>(), GlobalPosition, Data, mouseEvent.Position);
            AcceptEvent();
        }
    }
}