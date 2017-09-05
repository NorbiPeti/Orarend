using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Orarend
{
    [DataContract]
    public class ÓraTípus
    {
        [DataMember]
        private string teljesnév;
        public string TeljesNév
        {
            get
            {
                return teljesnév;
            }
            set
            {
                if ((value?.Trim()?.Length ?? 0) == 0)
                    teljesnév = "(" + Azonosító + ")";
                else
                    teljesnév = value;
            }
        }
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
