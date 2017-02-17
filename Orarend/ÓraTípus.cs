using System.Collections.Generic;

namespace Orarend
{
    public class ÓraTípus
    {
        public string TeljesNév { get; set; }
        public string Azonosító { get; set; }
        public string EgyediNév { get; set; }

        /// <summary>
        /// A kulcs az óra azonosítója
        /// </summary>
        public static Dictionary<string, ÓraTípus> Típusok { get; } = new Dictionary<string, ÓraTípus>();
    }
}
