using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend.Events
{
    public class TimerEventArgs : EventArgs
    {
        /// <summary>
        /// Lehet null
        /// </summary>
        public string KövetkezőÓra { get; }
        /// <summary>
        /// Lehet null
        /// </summary>
        public string HátralévőIdő { get; }
        public TimerEventArgs(string kövóra, string hátralévőidő)
        {
            KövetkezőÓra = kövóra;
            HátralévőIdő = hátralévőidő;
        }
    }
}
