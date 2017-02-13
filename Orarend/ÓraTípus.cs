using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    public class ÓraTípus
    {
        public string TeljesNév { get; set; }
        public string Azonosító { get; set; }
        public string EgyediNév { get; set; }

        public static Dictionary<string, ÓraTípus> Típusok { get; } = new Dictionary<string, ÓraTípus>();
    }
}
