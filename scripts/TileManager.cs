using Godot;
using System;
using System.Collections.Generic;

public partial class TileManager : Node
{
    public List<TileData> TodasLasLosetas = new List<TileData>();

    // Ahora recibe un string con la fase ("A", "B" o "C")
    public void CargarLosetas(string fase = "A")
    {
        // Limpiamos la lista por si se vuelve a llamar (ej. al cambiar de fase)
        TodasLasLosetas.Clear();

        // Construimos la ruta dinámicamente
        string rutaBase = $"res://assets/losetas/{fase}/";
        
        Dictionary<string, (TileData.TileType Type, string Desc)> mapaLosetas = new Dictionary<string, (TileData.TileType, string)>
        {
            {"loseta_01", (TileData.TileType.Special, "Edificio Azul (❤️ + 💧 + $)")},
            {"loseta_02", (TileData.TileType.Energy, "Satélite ($)")},
            {"loseta_03", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            {"loseta_04", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            {"loseta_05", (TileData.TileType.Heart, "Invernadero (❤️ Verde)")},
            {"loseta_06", (TileData.TileType.Hazard, "Triángulo Peligro (⚠️)")},
            {"loseta_07", (TileData.TileType.Heart, "Invernadero (❤️ Verde)")},
            {"loseta_08", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"loseta_09", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            {"loseta_10", (TileData.TileType.Heart, "Invernadero (❤️ Verde)")},
            {"loseta_11", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"loseta_12", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            {"loseta_13", (TileData.TileType.Special, "Módulo (+2 ❤️)")},
            {"loseta_14", (TileData.TileType.Special, "Edificio Azul (❤️ + 💧)")},
            {"loseta_15", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"loseta_16", (TileData.TileType.Energy, "Panel Solar (⚡ doble)")},
            {"loseta_17", (TileData.TileType.Energy, "Satélite ($)")},
            {"loseta_18", (TileData.TileType.Energy, "Satélite ($)")},
            {"loseta_19", (TileData.TileType.Energy, "Satélite ($)")},
            {"loseta_20", (TileData.TileType.Hazard, "Excavadora (⚠️)")},
            {"loseta_21", (TileData.TileType.Heart, "Invernadero (❤️ Naranja)")},
            {"loseta_22", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"loseta_23", (TileData.TileType.Special, "Módulo (+2 ❤️)")},
            {"loseta_24", (TileData.TileType.Special, "Módulo (+2 ⚫)")},
            {"loseta_25", (TileData.TileType.Special, "Edificio Azul (❤️ + 💧)")},
            {"loseta_26", (TileData.TileType.Blackhole, "Asteroide (⚫)")},
            {"loseta_27", (TileData.TileType.Hazard, "Excavadora (⚠️)")},
            {"loseta_28", (TileData.TileType.Heart, "Invernadero (❤️ Amarillo)")},
            {"loseta_29", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"loseta_30", (TileData.TileType.Special, "Rover (Especial)")},
            {"loseta_31", (TileData.TileType.Blackhole, "Asteroide (⚫)")},
            {"loseta_32", (TileData.TileType.Special, "Módulo (+2 ⚫)")},
            {"loseta_33", (TileData.TileType.Special, "Edificio Azul (❤️ + 💧)")}
        };

        foreach (var item in mapaLosetas)
        {
            // Carga desde la carpeta de la fase actual (ej: res://assets/losetas/A/loseta_01.png)
            Texture2D textura = GD.Load<Texture2D>(rutaBase + item.Key + ".png");
            if (textura != null)
            {
                TileData nuevaLoseta = new TileData();
                nuevaLoseta.TileId = item.Key;
                nuevaLoseta.Type = item.Value.Type;
                nuevaLoseta.Description = item.Value.Desc;
                nuevaLoseta.TileImage = textura;
                TodasLasLosetas.Add(nuevaLoseta);
            }
            else
            {
                GD.PrintErr($"No se encontró la loseta: {rutaBase + item.Key}.png");
            }
        }
        
        GD.Print($"¡Carga de losetas completada! Fase: {fase} | Total: {TodasLasLosetas.Count}");
    }
}