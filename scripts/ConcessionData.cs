using Godot;
using System;

[GlobalClass]
public partial class ConcessionData : Resource
{
    [Export] public string ConcessionId { get; set; }
    [Export] public Texture2D ConcessionImage { get; set; }
    
    public enum ConcessionColor { Green, Red }
    [Export] public ConcessionColor Color { get; set; }
    
    [Export] public int RequiredPoints { get; set; }
    [Export] public int VictoryPoints { get; set; }
}