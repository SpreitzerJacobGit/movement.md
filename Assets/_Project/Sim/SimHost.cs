using System;

namespace MovementMD.Sim
{
    /// <summary>
    /// Process-wide accessor to the active <see cref="ISimContext"/>. Defaults to a
    /// <see cref="NullSimContext"/> until the Quantum host is integrated, then a Quantum-side
    /// component calls <see cref="Set"/> on awake to install itself.
    /// </summary>
    /// <remarks>
    /// Named <c>SimHost</c> (not <c>Sim</c>) to avoid a namespace/type collision: a class named
    /// <c>Sim</c> inside namespace <c>MovementMD.Sim</c> is shadowed by the namespace itself when
    /// referenced from any code under <c>MovementMD.*</c>, which resolves <c>Sim.X</c> as
    /// <c>MovementMD.Sim.X</c> (a namespace lookup) instead of the class.
    /// </remarks>
    public static class SimHost
    {
        private static ISimContext s_current = new NullSimContext();

        public static ISimContext Current => s_current;

        public static void Set(ISimContext context)
            => s_current = context ?? throw new ArgumentNullException(nameof(context));
    }
}
