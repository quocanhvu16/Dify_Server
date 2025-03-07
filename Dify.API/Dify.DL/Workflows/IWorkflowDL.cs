using Dify.Common.Entities;
using Dify.DL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.DL.Workflows
{
    public interface IWorkflowDL : IBaseDL
    {
        Task<List<Workflow>> GetAllWorkFlowDraft();
        Task<Workflow> GetWorkFlowDraftByID(string id);
        Task<string> SyncWorkflowDraft(Workflow workflow);
    }
}
