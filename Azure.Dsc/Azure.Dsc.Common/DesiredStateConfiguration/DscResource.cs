using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public class DscResource
    {
        public DscResource()
        {
            Classes = new List<DscResourceClass>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string ModuleName
        {
            get { return ModulePath.Substring(ModulePath.LastIndexOf('\\') + 1); }
        }
        public string ModulePath { get; set; }

        public string ResourceName
        {
            get { return ResourcePath.Substring(ResourcePath.LastIndexOf('\\') + 1); }
        }
        public string ResourcePath { get; set; }

        public string FriendlyName { get; set; }

        public List<DscResourceClass> Classes { get; set; }

        public string PrettyName
        {
            get { return string.Format(@"{0}\{1}", ModuleName, FriendlyName); }
            set { }
        }

        public override string ToString()
        {
            return string.Format(@"{0}\{1}", ModuleName, FriendlyName);
        }
    }
}
