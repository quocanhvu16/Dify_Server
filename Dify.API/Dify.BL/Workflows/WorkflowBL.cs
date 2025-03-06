using Dify.BL.Base;
using Dify.Common.Cache;
using Dify.Common.Constant;
using Dify.Common.Entities;
using Dify.Common.MessageQ;
using Dify.Common.Model;
using Dify.Common.QueueData;
using Dify.DL.Base;
using Dify.DL.Workflows;
using Microsoft.EntityFrameworkCore;
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

        public async Task<string> RunWorkflowV2(RunWorkflowModel workflowRun)
        {
            var workflowID = workflowRun.WorkflowID;

            // Lấy ra dữ liệu workflow
            var workflow =  await GetWorkflowDraftByID(workflowID);

            // Chuẩn bị dữ liệu cache
            await _serviceCached.SetCacheAsync(string.Format(CacheString.WorkflowInfo,workflowID), workflow);

            // Lookup tìm node cho nhanh
            var nodeLookup = new Dictionary<string, Node>();
            foreach (var node in workflow.Graph.Nodes)
            {
                nodeLookup[node.NodeID] = node;
            }
            await _serviceCached.SetCacheAsync(string.Format(CacheString.WorkflowNode,workflowID), nodeLookup);

            // Xây dựng dependency graph từ các edges
            var dependencies = new Dictionary<string, List<string>>();
            foreach (var edge in workflow.Graph.Edges)
            {
                if (!dependencies.ContainsKey(edge.Source))
                {
                    dependencies[edge.Source] = new List<string>();
                }
                dependencies[edge.Source].Add(edge.Target);
            }
            await _serviceCached.SetCacheAsync(string.Format(CacheString.WorkflowDependency, workflowID), dependencies);

            // Thêm node bắt đầu vào hàng chờ
            var startNode = workflow.Graph.Nodes.FirstOrDefault(n => n.Data.Type == "start");

            // Context dữ liệu các node
            var context = new Dictionary<string, object> ();
            context.Add(startNode.NodeID, workflowRun.Inputs);
            await _serviceCached.SetCacheAsync(string.Format(CacheString.WorkflowContext, workflowID), context);

            // Đẩy Start Node vào hàng đợi RabbitMQ
            var nodeReady = new WorkflowQueueData
            {
                WorkflowID = workflowID,
                NodeID = startNode.NodeID
            };
            await _workflowPublisher.PublishAsync(workflowID, nodeReady);

            return workflowID;

        }
    }
}
