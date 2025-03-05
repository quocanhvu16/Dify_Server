using Dify.BL.Base;
using Dify.Common.Entities;
using Dify.DL.Base;
using Dify.DL.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.BL.Workflows
{
    public class WorkflowBL : BaseBL, IWorkflowBL
    {
        private readonly IWorkflowDL _workflowDL;
        public WorkflowBL(IWorkflowDL workflowDL) : base(workflowDL)
        {
            _workflowDL = workflowDL;
        }

        public async Task<List<Workflow>> GetAllWorkFlowDraft()
        {
            return await _workflowDL.GetAllWorkFlowDraft();
        }

        public async Task<Workflow> GetWorkflowDraftByID(string id)
        {
            return await _workflowDL.GetWorkFlowDraftByID(id);
        }
    }
}
