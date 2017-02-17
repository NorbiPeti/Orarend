using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    public class Settings
    {
        public bool DarkTheme { get; set; }
        public void UseCommonNames()
        {
            set("mateme", "Matek");
            set("prgelm", "Programozás elmélet");
            set("magny", "Nyelvtan");
            set(";prggy", "Programozás gyakorlat");
            set("testns", "Tesi");
            set("tapism", "Töri");
            set("matema", "Matek");
            set("bioege", "Biosz");
            set("foldra", "Föci");
            set(";halgy", "Hálózat gyakorlat");
        }

        private void set(string id, string name)
        {
            if (ÓraTípus.Típusok.ContainsKey(id))
                ÓraTípus.Típusok[id].EgyediNév = name;
        }
    }
}
