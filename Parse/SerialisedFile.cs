using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    public class ParseFileReference
    {
        public ParseFileReference(ParseFile parseFile)
        {
            name = parseFile.Name;
        }
        
        public readonly String __type = "File";
        public String name;
    }
}
