using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public class Óra
    {
        private ÓraTípus Típus { get; set; }
        [DataMember]
        public Tanár Tanár { get; set; }
        [DataMember]
        public string Terem { get; set; }
        /// <summary>
        /// Az órán résztvevő csoportok
        /// </summary>
        [DataMember]
        public string[] Csoportok { get; set; }

        [DataMember]
        public bool ManuálisanHozzáadott { get; set; }
     
        [DataMember]
        public string Azonosító
        {
            get
            {
                return Típus?.Azonosító;
            }
            set
            {
                if (!ÓraTípus.Típusok.ContainsKey(value))
                    ÓraTípus.Típusok.Add(value, Típus = new ÓraTípus { Azonosító = value });
                else
                    Típus = ÓraTípus.Típusok[value];
            }
        }

        public string TeljesNév
        {
            get
            {
                return Típus?.TeljesNév;
            }
            set
            {
                if (Típus == null)
                    throw new InvalidOperationException("Az azonosító nincs beállítva!");
                Típus.TeljesNév = value;
            }
        }

        public string EgyediNév
        {
            get
            {
                return Típus?.EgyediNév ?? Típus?.TeljesNév;
            }
            set
            {
                if (Típus == null)
                    throw new InvalidOperationException("Az azonosító nincs beállítva!");
                Típus.EgyediNév = value;
            }
        }

        public bool VanEgyediNév
        {
            get
            {
                return Típus?.EgyediNév == null;
            }
        }

        public void EgyediNévTörlése()
        {
            if (Típus != null)
                Típus.EgyediNév = null;
        }
    }
}
