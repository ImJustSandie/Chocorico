using UnityEngine;

/// <summary>
/// Coordina el spawn de objetos en las paredes entre todos los WallObjectSpawner.
/// Reglas:
/// - Solo puede haber UN grupo de púas activo en TODO el juego (evita púas enfrentadas).
/// - En una pared no puede haber púas y aguardiente a la vez (un aguardiente nunca
///   aparece sobre las púas, ni las púas sobre un aguardiente).
/// </summary>
public static class WallSpawnCoordinator
{
    public enum Side
    {
        Left,
        Right
    }

    private static readonly int[] activePuas = new int[2];
    private static readonly int[] activeAguardientes = new int[2];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        activePuas[0] = activePuas[1] = 0;
        activeAguardientes[0] = activeAguardientes[1] = 0;
    }

    private static bool HasPuasAnywhere => (activePuas[0] + activePuas[1]) > 0;
    private static bool HasPuasOn(Side side) => activePuas[(int)side] > 0;
    private static bool HasAguardienteOn(Side side) => activeAguardientes[(int)side] > 0;

    /// <summary>
    /// Un grupo de púas puede aparecer solo si no hay otras púas activas en ninguna
    /// pared y no hay un aguardiente en esa misma pared.
    /// </summary>
    public static bool CanSpawnPua(Side side)
    {
        return !HasPuasAnywhere && !HasAguardienteOn(side);
    }

    /// <summary>
    /// Un aguardiente puede aparecer solo si su pared está libre de púas y de otros aguardientes.
    /// </summary>
    public static bool CanSpawnAguardiente(Side side)
    {
        return !HasPuasOn(side) && !HasAguardienteOn(side);
    }

    public static void RegisterPua(Side side)
    {
        activePuas[(int)side]++;
    }

    public static void UnregisterPua(Side side)
    {
        activePuas[(int)side] = Mathf.Max(0, activePuas[(int)side] - 1);
    }

    public static void RegisterAguardiente(Side side)
    {
        activeAguardientes[(int)side]++;
    }

    public static void UnregisterAguardiente(Side side)
    {
        activeAguardientes[(int)side] = Mathf.Max(0, activeAguardientes[(int)side] - 1);
    }
}
