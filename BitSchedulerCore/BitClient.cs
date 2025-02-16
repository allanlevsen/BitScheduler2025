using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSchedulerCore
{
    public class BitClient
    {
        public int BitClientId { get; set; }
        public string Name { get; set; }
        public ICollection<BitResource> BitResources { get; set; } = new List<BitResource>();
        public ICollection<BitReservation> BitReservations { get; set; } = new List<BitReservation>();
    }
}
