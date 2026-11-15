/* 
    Gestiona la carga y consulta de las conesiones Verdes.
    Define los datos de cada carta (ID, puntos, requisitos y efecto), carga sus imágenes y las almacena en un diccionario.
    Permite obtener una carta mediante su ID. Requiere imágenes PNG en "res://assets/concesiones_verdes/".

    Se definen los valores Central y Efecto en el diccionario, pero todavía no los transfiere a ConcessionData.
*/
using Godot;
using System.Collections.Generic;

public partial class ConcesionesVerdes : Node
{
    public Dictionary<string, ConcessionData> Cartas = new();

    private const string Ruta =
        "res://assets/concesiones_verdes/";

    public void CargarCartas()
    {
        Cartas.Clear();

        var datos = new Dictionary<string,
            (int Superior, int Inferior, string Efecto)>
        {
            {"Carta_001", (13, 6, "Movimiento4_CuboAzul")},
            {"Carta_002", (3, 8, "Personajes_Recursos")},
            {"Carta_003", (9, 6, "Conexion_Crater")},
            {"Carta_004", (11, 6, "Movimiento4_Edificio")},
            {"Carta_005", (2, 8, "X_EstructuraMorada")},
            {"Carta_006", (10, 7, "Diagonal_Crater")},
            {"Carta_007", (1, 8, "Cuadricula3x3")},
            {"Carta_008", (7, 8, "Conexion_Bateria")},
            {"Carta_009", (14, 7, "Diagonal_Bateria")},
            {"Carta_010", (4, 7, "Conexion_Triangulo")},
            {"Carta_011", (8, 6, "Conexion_Generico")},
            {"Carta_012", (8, 6, "Diagonal_Generico")},
            {"Carta_013", (8, 6, "Diagonal_Generico")},
            {"Carta_014", (8, 7, "Conexion_Generico")},
            {"Carta_015", (8, 7, "Conexion_Generico")}
        };

        foreach (var item in datos)
        {
            Texture2D imagen = GD.Load<Texture2D>(
                Ruta + item.Key + ".png"
            );

            Cartas[item.Key] = new ConcessionData
            {
                ConcessionId = item.Key,
                Color = ConcessionData.ConcessionColor.Green,
                VictoryPoints = item.Value.Superior,
                RequiredPoints = item.Value.Inferior,
                ConcessionImage = imagen
            };

            if (imagen == null)
                GD.PushWarning($"Imagen no encontrada: {item.Key}");
        }

        GD.Print($"Concesiones verdes cargadas: {Cartas.Count}");
    }

    public ConcessionData ObtenerCarta(string id)
    {
        return Cartas.TryGetValue(id, out var carta)
            ? carta
            : null;
    }
}