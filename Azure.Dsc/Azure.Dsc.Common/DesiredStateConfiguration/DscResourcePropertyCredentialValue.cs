using Sogeti.IaC.Model.DesiredStateConfiguration;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public class DscResourcePropertyCredentialValue : DscResourcePropertyValue
    {
        private string _value;

        public override string Value
        {
            get
            {
                return string.Format(@"New-Object System.Management.Automation.PSCredential (""{0}"", (ConvertTo-SecureString ""{1}"" -AsPlainText -Force))", UserName, Password);
            }
            set { _value = value; }
        }
        public string UserName { get; set; }
        public string Password { get; set; }
        
    }
}
