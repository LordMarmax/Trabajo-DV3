using Godot;
using System;

[GlobalClass]
public partial class TileData : Resource
{
    [Export] public string TileId { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D TileImage { get; set; }
    
    public enum TileType { Heart, Energy, Hazard, Blackhole, Special }
    [Export] public TileType Type { get; set; }

    // NUEVO: Para saber el tamaño de la loseta y su fase
    [Export] public string Shape { get; set; } = "1x1"; // "1x1" o "2x1"
    [Export] public string Phase { get; set; } = "A";
}