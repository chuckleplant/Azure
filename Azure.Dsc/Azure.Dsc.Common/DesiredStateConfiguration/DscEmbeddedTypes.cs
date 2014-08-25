using System.Collections.Generic;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public class DscEmbeddedTypes
    {
        private readonly Dictionary<string, string> _embeddedTypes = new Dictionary<string, string>();

        public string this[string type]
        {
            get { return _embeddedTypes[type]; }
        }

        public DscEmbeddedTypes()
        {
            Initialize();
        }
        private void Initialize()
        {
            _embeddedTypes.Add("Hashtable", "MSFT_KeyValuePair");
            _embeddedTypes.Add("PSCredential", "MSFT_Credential");
            _embeddedTypes.Add("Hashtable[]", "MSFT_KeyValuePair");
            _embeddedTypes.Add("PSCredential[]", "MSFT_Credential");
            _embeddedTypes.Add("HashtableArray", "MSFT_KeyValuePair");
            _embeddedTypes.Add("PSCredentialArray", "MSFT_Credential");
        }
    }
}
