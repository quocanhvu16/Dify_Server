using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Model
{
    public class RunWorkflowModel
    {
        public string WorkflowID { get; set; }
        public Dictionary<string, object> Inputs { get; set; }
    }
}
