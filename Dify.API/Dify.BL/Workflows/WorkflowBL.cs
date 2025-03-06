using Dify.BL.Base;
using Dify.Common.Cache;
using Dify.Common.Entities;
using Dify.Common.MessageQ;
using Dify.Common.Model;
using Dify.Common.QueueData;
using Dify.DL.Base;
using Dify.DL.Workflows;
using Microsoft.Extensions.Caching.Distributed;
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

        private readonly ServiceCached _serviceCached;

        private readonly IPublisher<WorkflowQueueData> _workflowPublisher;

        public WorkflowBL(IWorkflowDL workflowDL, ServiceCached serviceCached, IPublisher<WorkflowQueueData> workflowPublisher) : base(workflowDL)
        {
            _workflowDL = workflowDL;
            _serviceCached = serviceCached;
            _workflowPublisher = workflowPublisher;
        }

        public async Task<List<Workflow>> GetAllWorkFlowDraft()
        {
            return await _workflowDL.GetAllWorkFlowDraft();
        }

        public async Task<Workflow> GetWorkflowDraftByID(string id)
        {
            return await _workflowDL.GetWorkFlowDraftByID(id);
        }

        public async Task<string> RunWorkflowV2(RunWorkflowModel workflow)
        {
            var workflowId = workflow.WorkflowID;

            // Lưu trạng thái workflow vào Redis
            await _serviceCached.SetCacheAsync($"workflow:{workflowId}:status", "pending");

            // Đẩy Start Node vào hàng đợi RabbitMQ
            var startNode = new WorkflowQueueData
            {
               
            };
            await _workflowPublisher.PublishAsync("WorkflowQueue", startNode);

            return workflowId;

        }
    }
}
