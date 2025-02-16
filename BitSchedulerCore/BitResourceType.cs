using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSchedulerCore
{
    public class BitResourceType
    {
        public int BitResourceTypeId { get; set; }
        public string Name { get; set; }

        public ICollection<BitResource> BitResources { get; set; }
    }
}
