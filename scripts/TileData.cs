using Godot;
using System;

[GlobalClass]
public partial class TileData : Resource
{
    [Export] public string TileId { get; set; }
    [Export] public Texture2D TileImage { get; set; }
    
    public enum TileType { None, Heart, Energy, Hazard, Blackhole, Special }
    [Export] public TileType Type { get; set; } = TileType.None;
}