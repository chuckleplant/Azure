using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Azure.Dsc.Common.DesiredStateConfiguration
{
    public class DscTypeMapper
    {
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public Type this[string type]
        {
            get { return _types[type]; }
        }

        public DscTypeMapper()
        {
            Initialize();
        }
        private void Initialize()
        {
            _types.Add("Uint8", typeof(byte));
            _types.Add("Uint16", typeof(UInt16));
            _types.Add("Uint32", typeof(UInt32));
            _types.Add("Uint64", typeof(UInt64));
            _types.Add("Sint8", typeof(sbyte));
            _types.Add("Sint16", typeof(Int16));
            _types.Add("Sint32", typeof(Int32));
            _types.Add("Sint64", typeof(Int64));
            _types.Add("Real32", typeof(Single));
            _types.Add("Real64", typeof(double));
            _types.Add("Char16", typeof(char));
            _types.Add("String", typeof(string));
            _types.Add("Boolean", typeof(bool));
            _types.Add("DateTime", typeof(DateTime));
            _types.Add("Hashtable", typeof(Hashtable));
            _types.Add("PSCredential", typeof(PSCredential));

            _types.Add("Uint8[]", typeof(byte[]));
            _types.Add("Uint16[]", typeof(UInt16[]));
            _types.Add("Uint32[]", typeof(UInt32[]));
            _types.Add("Uint64[]", typeof(UInt64[]));
            _types.Add("Sint8[]", typeof(sbyte[]));
            _types.Add("Sint16[]", typeof(Int16[]));
            _types.Add("Sint32[]", typeof(Int32[]));
            _types.Add("Sint64[]", typeof(Int64[]));
            _types.Add("Real32[]", typeof(Single[]));
            _types.Add("Real64[]", typeof(double[]));
            _types.Add("Char16[]", typeof(char[]));
            _types.Add("String[]", typeof(string[]));
            _types.Add("Boolean[]", typeof(bool[]));
            _types.Add("DateTime[]", typeof(DateTime[]));
            _types.Add("Hashtable[]", typeof(Hashtable[]));
            _types.Add("PSCredential[]", typeof(PSCredential[]));
        }
    }
}
