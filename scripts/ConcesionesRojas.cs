/* 
    Gestiona la carga y consulta de las conesiones Rojas.
    Define los datos de cada carta (ID, puntos, requisitos y efecto), carga sus imágenes y las almacena en un diccionario.
    Permite obtener una carta mediante su ID. Requiere imágenes PNG en "res://assets/concesiones_rojas/".

    Se definen los valores Central y Efeto en el diccionario, pero todavía no los transfiere a ConcessionData.
*/

using Godot;
using System.Collections.Generic;

public partial class ConcesionesRojas : Node
{
    public Dictionary<string, ConcessionData> Cartas = new();

    private const string Ruta =
        "res://assets/concesiones_rojas/";

    public void CargarCartas()
    {
        Cartas.Clear();

        // ID, valor superior, inferior, central y símbolo
        var datos = new Dictionary<string,
            (int Superior, int Inferior, int? Central, string Efecto)>
        {
            {
                "01_asteroide",
                (7, 6, null, "Asteroide")
            },
            {
                "01_cuadricula_roja",
                (2, 10, null, "Cuadricula3x3")
            },
            {
                "01_robot_medico",
                (2, 9, 5, "RobotMedico")
            },
            {
                "02_antena_satelital",
                (6, 8, 4, "AntenaSatelital")
            },
            {
                "02_columnas_rojas",
                (9, 10, null, "ColumnasRojas")
            },
            {
                "02_cuatro_iconos",
                (3, 10, null, "CuatroIconos")
            },
            {
                "03_capsula_azul",
                (5, 10, 5, "CapsulaAzul")
            },
            {
                "03_personajes",
                (8, 10, null, "Personajes")
            },
            {
                "03_torre_azul",
                (4, 9, 5, "TorreAzul")
            }
        };

        foreach (var item in datos)
        {
            string id = item.Key;

            Texture2D imagen = GD.Load<Texture2D>(
                Ruta + id + ".png"
            );

            Cartas[id] = new ConcessionData
            {
                ConcessionId = id,
                Color = ConcessionData.ConcessionColor.Red,

                VictoryPoints = item.Value.Superior,
                RequiredPoints = item.Value.Inferior,

                ConcessionImage = imagen
            };

            if (imagen == null)
            {
                GD.PushWarning(
                    $"No se encontró la imagen: {id}"
                );
            }
        }

        GD.Print(
            $"Concesiones rojas cargadas: {Cartas.Count}"
        );
    }

    public ConcessionData ObtenerCarta(string id)
    {
        if (Cartas.TryGetValue(id, out var carta))
        {
            return carta;
        }

        GD.PushWarning(
            $"Concesión roja desconocida: {id}"
        );

        return null;
    }
}
