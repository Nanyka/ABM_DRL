using System.Collections.Generic;

namespace Sugarscape
{
    public interface ITerrain
    {
        public IEnumerable<GridCell> Cells { get; }
    }
}