using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSchedulerCore
{
    public class BitResource
    {
        public int BitResourceId { get; set; }
        public int BitResourceTypeId { get; set; }
        public BitResourceType BitResourceType { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public int BitClientId { get; set; }
        public BitClient BitClient { get; set; }
        public ICollection<BitReservation> BitReservations { get; set; } = new List<BitReservation>();
    }
}
