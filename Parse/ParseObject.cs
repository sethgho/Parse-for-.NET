/*
    Copyright (c) 2011 Alastair Paterson

    Permission is hereby granted, free of charge, to any person obtaining a copy of this
    software and associated documentation files (the "Software"), to deal in the Software
    without restriction, including without limitation the rights to use, copy, modify,
    merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be included in all copies
    or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    public class ParseObject : Dictionary<String,Object>
    {
        public String createdAt 
        {
            get
            {
                return base["createdAt"].ToString();
            }
            set
            {
                base["createdAt"] = value;
            }
        }

        public String objectId
        {
            get
            {
                return (String)base["objectId"];
            }
            set
            {
                base["objectId"] = value;
            }
        }

        public String Class
        {
            get
            {
                return (String)base["Class"];
            }
            set
            {
                base["Class"] = value;
            }
        }

        public ParseObject(String ClassName)
        {
            Class = ClassName;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateFromKey(String KeyName)
        {
            return DateTime.ParseExact(this[KeyName].ToString(), "yyyy-MM-ddTHH:mm:ss.fffZ", null);
        }

        public virtual new void Add(String key, Object value)
        {
            if (value.GetType() == typeof(DateTime))
            {
                base.Add(key, new SerialisedDate((DateTime)value));
            }
            else if (value.GetType() == typeof(ParseFile))
            {
                base.Add(key, new ParseFileReference((ParseFile)value));
            }

            else base.Add(key, value);
        }
    }
}
