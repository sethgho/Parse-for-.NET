using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Parse
{
    public class ParseFileReference
    {
        public ParseFileReference(ParseFile parseFile)
        {
            name = Path.GetFileName(parseFile.Url);
        }
        
        public readonly String __type = "File";
        public String name;
    }
}
