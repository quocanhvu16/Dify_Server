using Dify.BL.Base;
using Dify.Common.Entities;
using Dify.Common.Model;

namespace Dify.BL.Workflows
{
    public interface IWorkflowBL : IBaseBL
    {
        Task<List<Workflow>> GetAllWorkFlowDraft();
        Task<Workflow> GetWorkflowDraftByID(string id);

        Task<string> RunWorkflowV2(RunWorkflowModel workflow);
    }
}
