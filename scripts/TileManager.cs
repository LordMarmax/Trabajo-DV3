using Godot;
using System;
using System.Collections.Generic;

public partial class TileManager : Node
{
    public List<TileData> TodasLasLosetas = new List<TileData>();

    public void CargarLosetas()
    {
        string rutaBase = "res://assets/losetas/";
        
        // Diccionario con el nombre del archivo, su tipo y una descripción
        Dictionary<string, (TileData.TileType Type, string Desc)> mapaLosetas = new Dictionary<string, (TileData.TileType, string)>
        {
            // --- Image 1 ---
            {"carta_01(1)", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"carta_01(2)", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"carta_01(3)", (TileData.TileType.Special, "Edificio Azul (❤️ + 💎)")},
            {"carta_01(4)", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"carta_01", (TileData.TileType.Heart, "Hábitat Robot (❤️)")},
            {"carta_02(1)", (TileData.TileType.Special, "Rover (Especial)")},
            {"carta_02(2)", (TileData.TileType.Energy, "Panel Solar (⚡)")},
            {"carta_02(3)", (TileData.TileType.Energy, "Satélite ($)")},
            {"carta_02(4)", (TileData.TileType.Special, "Módulo (+2 ❤️)")},
            {"carta_02", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            {"carta_03(1)", (TileData.TileType.Blackhole, "Asteroide (⚫)")},
            {"carta_03(2)", (TileData.TileType.Energy, "Satélite ($)")},
            {"carta_03(3)", (TileData.TileType.Heart, "Tanque Agua (❤️)")},
            {"carta_03(4)", (TileData.TileType.Special, "Módulo (+2 ❤️)")},
            {"carta_03", (TileData.TileType.Heart, "Invernadero (❤️ Rosa)")},
            {"carta_04(1)", (TileData.TileType.Special, "Módulo (+2 ❤️)")},
            {"carta_04(2)", (TileData.TileType.Energy, "Satélite ($)")},
            {"carta_04(3)", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            
            // --- Image 2 ---
            {"carta_04(4)", (TileData.TileType.Special, "Edificio Azul (❤️ + 💧)")},
            {"carta_04", (TileData.TileType.Special, "Hábitat Robot (❤️ + 💧)")},
            {"carta_05(1)", (TileData.TileType.Heart, "Edificio Azul (❤️)")},
            {"carta_05(2)", (TileData.TileType.Energy, "Satélite ($)")},
            {"carta_05(3)", (TileData.TileType.Heart, "Invernadero (❤️ Rosa)")},
            {"carta_05(4)", (TileData.TileType.Blackhole, "Asteroide (⚫)")},
            {"carta_05", (TileData.TileType.Special, "Tanque Agua (❤️ + ⚫)")},
            {"carta_06(1)", (TileData.TileType.Hazard, "Excavadora (⚠️)")},
            {"carta_06(2)", (TileData.TileType.Hazard, "Triángulo Peligro (⚠️)")},
            {"carta_06(3)", (TileData.TileType.Hazard, "Excavadora (⚠️)")},
            {"carta_06", (TileData.TileType.Special, "Módulo (+2 ❤️)")},
            {"carta_07(1)", (TileData.TileType.Heart, "Invernadero (❤️ Naranja)")},
            {"carta_07(2)", (TileData.TileType.Heart, "Invernadero (❤️ Rosa)")},
            {"carta_07(3)", (TileData.TileType.Special, "Invernadero (❤️ + 💧)")},
            {"carta_07", (TileData.TileType.Special, "Edificio Azul (❤️ + 💧)")}
        };

        foreach (var item in mapaLosetas)
        {
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
                GD.PrintErr($"No se encontró la loseta: {item.Key}.png");
            }
        }
        
        GD.Print($"¡Carga de losetas completada! Total: {TodasLasLosetas.Count}");
    }
}