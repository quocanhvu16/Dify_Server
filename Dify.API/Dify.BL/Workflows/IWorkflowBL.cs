using Dify.BL.Base;
using Dify.Common.Entities;

namespace Dify.BL.Workflows
{
    public interface IWorkflowBL : IBaseBL
    {
        Task<List<Workflow>> GetAllWorkFlowDraft();
        Task<Workflow> GetWorkflowDraftByID(string id);
    }
}
