using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    public class Óra
    {
        private ÓraTípus Típus { get; set; }
        public Tanár Tanár { get; set; }
        public int Sorszám { get; set; }
        public string Terem { get; set; }
        /// <summary>
        /// Az órán résztvevő csoportok
        /// </summary>
        public string[] Csoportok { get; set; }

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
    }
}
