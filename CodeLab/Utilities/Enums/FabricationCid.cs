namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// CADmep pattern numbers (CID) of pipework parts, from Autodesk's Fabrication CID table.
    /// Read from a part with FabricationPart.ItemCustomId.
    /// Some numbers repeat on purpose, exactly as in Autodesk's table (2160, 2522): two names, same value.
    /// Side effect: ToString() of a repeated value may show either name.
    /// A CID that is not listed here still works: it becomes a number without a name.
    /// </summary>
    public enum FabricationCid
    {
        /// <summary>Hangers: 838, and also 1238 to 1250 (not listed one by one).</summary>
        Hanger = 838,

        Trap = 916,
        ValveGeneric = 868,

        Pipe = 2041,
        YBranchTee = 2042,
        Cross = 2044,
        Tee = 2047,
        ConcentricReducer = 2051,
        EccentricReducer = 2071,
        ReducingElbow = 2097,

        /// <summary>Same value as ReducingCross (Autodesk's table).</summary>
        ReducingTeeSanitary = 2160,

        /// <summary>Same value as ReducingTeeSanitary (Autodesk's table).</summary>
        ReducingCross = 2160,

        /// <summary>Couplings, adapters, unions and bushings. Same value as Strainer (Autodesk's table).</summary>
        CouplingAdapterUnionBushing = 2522,

        /// <summary>Same value as CouplingAdapterUnionBushing (Autodesk's table).</summary>
        Strainer = 2522,

        Elbow = 2523,
        Valve2Port = 2868,
        Valve3Port = 2869,
        SanitaryTee = 2882,
        ReducingTee = 2884,
        CombinationWyeEighthBend = 2900,
        DoubleCombinationWyeEighthBend = 2901
    }
}
