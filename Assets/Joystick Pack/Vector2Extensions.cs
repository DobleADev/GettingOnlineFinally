using UnityEngine;

// Es una buena práctica colocar las clases de extensión en un namespace descriptivo.
public static class Vector2Extensions
{
    /// <summary>
    /// Multiplica los componentes de dos Vector2 entre sí (componente a componente).
    /// </summary>
    /// <param name="v1">El primer Vector2.</param>
    /// <param name="v2">El segundo Vector2.</param>
    /// <returns>Un nuevo Vector2 con los componentes resultantes de la multiplicación.</returns>
    public static Vector2 MultiplyWith(this Vector2 v1, Vector2 v2)
    {
        return new Vector2(v1.x * v2.x, v1.y * v2.y);
    }
    /// <summary>
    /// Divide los componentes de dos Vector2 entre sí (componente a componente).
    /// </summary>
    /// <param name="v1">El dividendo Vector2.</param>
    /// <param name="v2">El divisor Vector2.</param>
    /// <returns>Un nuevo Vector2 con los componentes resultantes de la división.</returns>
    /// <remarks>Ten cuidado con divisiones por cero.</remarks>
    public static Vector2 DivideWith(this Vector2 v1, Vector2 v2)
    {
        // Puedes añadir aquí lógica para manejar divisiones por cero si es necesario,
        // por ejemplo, devolver float.NaN o 0 si el divisor es 0.
        return new Vector2(v1.x / v2.x, v1.y / v2.y);
    }
}