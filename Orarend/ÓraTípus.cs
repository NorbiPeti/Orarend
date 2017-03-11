using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Orarend
{
    [DataContract]
    public class ÓraTípus
    {
        [DataMember]
        public string TeljesNév { get; set; }
        [DataMember]
        public string Azonosító { get; set; }
        [DataMember]
        public string EgyediNév { get; set; }
        
        /// <summary>
        /// A kulcs az óra azonosítója
        /// </summary>
        public static Dictionary<string, ÓraTípus> Típusok { get { return API.példány.típusok; } }
    }
}
