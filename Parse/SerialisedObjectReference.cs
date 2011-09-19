using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    class SerialisedObjectReference
    {
        public String className { get; set; }
        public String objectId { get; set; }
        public String __type = "Pointer";
        public SerialisedObjectReference(String classIdentifier, String objectIdentifier)
        {
            className = classIdentifier;
            objectId = objectIdentifier;
        }
    }
}
