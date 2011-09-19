using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    public class SerialisedDate
    {
        public SerialisedDate(DateTime theDate)
        {
            iso = theDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public string __type = "Date";
        public string iso;
    }
}