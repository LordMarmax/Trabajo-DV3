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
            // Edificios Azules (Vitales — Torre)
            {"loseta_01", (TileData.TileType.VitalTorre,      "Edificio Azul (❤️ + 💧 + $)")},
            // Satélites (Oficinas de Venta $)
            {"loseta_02", (TileData.TileType.Oficina,         "Satélite ($)")},
            // Tanques de Agua (Vitales — Cúpula)
            {"loseta_03", (TileData.TileType.VitalCupula,     "Tanque Agua (❤️ + ⚫)")},
            {"loseta_04", (TileData.TileType.VitalCupula,     "Tanque Agua (❤️ + ⚫)")},
            // Invernaderos
            {"loseta_05", (TileData.TileType.InvernaderoVerde,"Invernadero (❤️ Verde)")},
            // Peligro
            {"loseta_06", (TileData.TileType.Hazard,          "Triángulo Peligro (⚠️)")},
            {"loseta_07", (TileData.TileType.InvernaderoVerde,"Invernadero (❤️ Verde)")},
            // Hábitat Robot (Vitales — Cubo)
            {"loseta_08", (TileData.TileType.VitalCubo,       "Hábitat Robot (❤️)")},
            {"loseta_09", (TileData.TileType.VitalCupula,     "Tanque Agua (❤️ + ⚫)")},
            {"loseta_10", (TileData.TileType.InvernaderoVerde,"Invernadero (❤️ Verde)")},
            {"loseta_11", (TileData.TileType.VitalCubo,       "Hábitat Robot (❤️)")},
            {"loseta_12", (TileData.TileType.VitalCupula,     "Tanque Agua (❤️ + ⚫)")},
            // Módulos Especiales (Especial +2/corazón)
            {"loseta_13", (TileData.TileType.EspecialModulo,  "Módulo (+2 ❤️)")},
            // Edificio Azul (Vital Torre)
            {"loseta_14", (TileData.TileType.VitalTorre,      "Edificio Azul (❤️ + 💧)")},
            {"loseta_15", (TileData.TileType.VitalCubo,       "Hábitat Robot (❤️)")},
            // Panel Solar (Oficina)
            {"loseta_16", (TileData.TileType.Oficina,         "Panel Solar (⚡ doble)")},
            // Satélites
            {"loseta_17", (TileData.TileType.Oficina,         "Satélite ($)")},
            {"loseta_18", (TileData.TileType.Oficina,         "Satélite ($)")},
            {"loseta_19", (TileData.TileType.Oficina,         "Satélite ($)")},
            // Excavadora (Acción / Peligro)
            {"loseta_20", (TileData.TileType.Accion,          "Excavadora (⚠️)")},
            // Invernaderos Naranja/Amarillo
            {"loseta_21", (TileData.TileType.InvernaderoNaranja,"Invernadero (❤️ Naranja)")},
            {"loseta_22", (TileData.TileType.VitalCubo,       "Hábitat Robot (❤️)")},
            // Módulo especial (complejo espacial)
            {"loseta_23", (TileData.TileType.EspecialComplejo,"Módulo (+2 ❤️)")},
            {"loseta_24", (TileData.TileType.EspecialModulo,  "Módulo (+2 ⚫)")},
            // Edificio Azul
            {"loseta_25", (TileData.TileType.VitalTorre,      "Edificio Azul (❤️ + 💧)")},
            // Meteoritos
            {"loseta_26", (TileData.TileType.Meteorito,       "Asteroide (⚫)")},
            // Excavadora Acción
            {"loseta_27", (TileData.TileType.Accion,          "Excavadora (⚠️)")},
            // Invernadero Amarillo
            {"loseta_28", (TileData.TileType.InvernaderoAmarillo,"Invernadero (❤️ Amarillo)")},
            {"loseta_29", (TileData.TileType.VitalCubo,       "Hábitat Robot (❤️)")},
            // Rover (Especial)
            {"loseta_30", (TileData.TileType.EspecialRover,   "Rover (Especial)")},
            // Meteorito
            {"loseta_31", (TileData.TileType.Meteorito,       "Asteroide (⚫)")},
            // Módulo
            {"loseta_32", (TileData.TileType.EspecialModulo,  "Módulo (+2 ⚫)")},
            // Edificio Azul
            {"loseta_33", (TileData.TileType.VitalTorre,      "Edificio Azul (❤️ + 💧)")}
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