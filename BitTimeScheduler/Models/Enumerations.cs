using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTimeScheduler.Models
{
    /// <summary>
    /// Metadata flags for a BitDay. For now, we only define IsFree.
    /// Additional flags can be added as needed.
    /// </summary>
    [Flags]
    public enum BitTimeMetadataFlags
    {
        None = 0,
        IsFree = 1 << 0
    }

}
