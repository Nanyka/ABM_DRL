using System;

namespace Sugarscape
{
    // Two optional extra actions that hand the step to a scripted rule in TradingAgent:
    // forage (best visible harvest) and seek a partner (walk toward a trader with a clearly different MRS).
    // Decided once at launch (mlagents-learn ... --env-args --seek-action) because the action count is part of the
    // behaviour spec and cannot change between episodes.
    public static class SeekAction
    {
        public const int ForageIndex = 5;
        public const int SeekIndex = 6;
        public static readonly bool Enabled = Array.IndexOf(Environment.GetCommandLineArgs(), "--seek-action") >= 0;
        public static int ActionCount => Enabled ? SeekIndex + 1 : ForageIndex;
    }
}
