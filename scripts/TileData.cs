using Godot;
using System;

[GlobalClass]
public partial class TileData : Resource
{
    [Export] public string TileId { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D TileImage { get; set; }
    
    public enum TileType
    {
        // Sistemas Vitales (corazón rojo)
        VitalCupula,     // cúpulas de agua / tanques
        VitalTorre,      // torres azules
        VitalCubo,       // cubos blancos / módulos
        // Invernaderos (frutas)
        InvernaderoVerde,
        InvernaderoNaranja,
        InvernaderoAmarillo,
        // Meteoritos
        Meteorito,
        // Oficinas de Venta (antena $)
        Oficina,
        // Construcciones Especiales (etiqueta amarilla)
        EspecialModulo,  // Módulo +2/corazón
        EspecialComplejo,// Complejo espacial
        EspecialRover,   // Rover u otros
        // Losetas de Acción (naranja)
        Accion,
        // Peligro / Andamio (no puntúan directamente)
        Hazard,
        Andamio,
        // Legado (Heart/Energy/etc. por compatibilidad temporal)
        Heart, Energy, Blackhole, Special
    }
    [Export] public TileType Type { get; set; }

    // NUEVO: Para saber el tamaño de la loseta y su fase
    [Export] public string Shape { get; set; } = "1x1"; // "1x1" o "2x1"
    [Export] public string Phase { get; set; } = "A";
}