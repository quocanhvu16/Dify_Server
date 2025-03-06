
using Dify.Common.Cache;
using Dify.Common.Constant;
using Dify.Common.Entities;
using Dify.Common.MessageQ;
using Dify.Common.Model;
using Dify.Common.QueueData;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;

namespace Dify.Console.API.Consumer
{
    public interface IRunWorkflowHandler
    {
        Task Handle(string key, WorkflowQueueData data);
    }

    public class RunWorkflowHandler : IRunWorkflowHandler
    {
        private readonly ServiceCached _serviceCached;

        private readonly IConnectionMultiplexer _redis;

        private readonly IPublisher<WorkflowQueueData> _workflowPublisher;

        public RunWorkflowHandler(ServiceCached serviceCached, IConnectionMultiplexer redis, IPublisher<WorkflowQueueData> workflowPublisher)
        {
            _serviceCached = serviceCached;
            _redis = redis;
            _workflowPublisher = workflowPublisher;
        }

        public async Task Handle(string key, WorkflowQueueData data)
        {
            try
            {
                await WorkflowRun(data);
            }
            catch (Exception ex)
            {

            }
        }

        public async Task WorkflowRun(WorkflowQueueData data)
        {
            var nodeLookup = await _serviceCached.GetCacheAsync<Dictionary<string, Node>>(string.Format(CacheString.WorkflowNode, data.WorkflowID));
            var dependencies = await _serviceCached.GetCacheAsync<Dictionary<string, List<string>>>(string.Format(CacheString.WorkflowDependency, data.WorkflowID));
            var workflow = await _serviceCached.GetCacheAsync<Workflow>(string.Format(CacheString.WorkflowInfo, data.WorkflowID));
            var completedNodes = new HashSet<string>();

            var node = nodeLookup[data.NodeID];
            var redisPub = _redis.GetSubscriber();
            object result = null;
            if (node.Data.Type == "start")
            {
                await redisPub.PublishAsync($"workflow:{data.WorkflowID}:events", JsonConvert.SerializeObject(HandleStream(StreamEvent.WORKFLOW_STARTED)));
            }
            else
            {
                completedNodes = await _serviceCached.GetCacheAsync<HashSet<string>>(string.Format(CacheString.WorkflowCompletedNode, data.WorkflowID));
            }

            try
            {
                await redisPub.PublishAsync($"workflow:{data.WorkflowID}:events", JsonConvert.SerializeObject(HandleStream(StreamEvent.NODE_STARTED, node.NodeID)));

                result = await ExecuteNodeAsync(node, data.WorkflowID);

                completedNodes.Add(data.NodeID);
                await _serviceCached.SetCacheAsync(string.Format(CacheString.WorkflowCompletedNode, data.WorkflowID), completedNodes);

                await redisPub.PublishAsync($"workflow:{data.WorkflowID}:events", JsonConvert.SerializeObject(HandleStream(StreamEvent.NODE_FINISHED, node.NodeID, new Result()
                {
                    Data = result
                })));
            }
            catch (Exception ex)
            {
                await redisPub.PublishAsync($"workflow:{data.WorkflowID}:events", JsonConvert.SerializeObject(HandleStream(StreamEvent.ERROR, node.NodeID, new Result()
                {
                    Success = false,
                    Error = $"Node {node.Data.Title} run failed: " + ex.Message
                })));

                await RemoveCache(data.WorkflowID);
            }

            // Đưa các node con vào hàng đợi nếu tất cả node cha của nó đã hoàn thành
            if (dependencies.ContainsKey(data.NodeID))
            {
                foreach (var childID in dependencies[data.NodeID])
                {
                    var allParentsCompleted = workflow.Graph.Edges
                        .Where(e => e.Target == childID)
                        .All(e => completedNodes.Contains(e.Source));

                    if (allParentsCompleted)
                    {
                        var nodeReady = new WorkflowQueueData
                        {
                            WorkflowID = data.WorkflowID,
                            NodeID = childID
                        };
                        await _workflowPublisher.PublishAsync(data.WorkflowID, nodeReady);
                    }
                }
            }

            if (node.Data.Type == "end")
            {
                await redisPub.PublishAsync($"workflow:{data.WorkflowID}:events", JsonConvert.SerializeObject(HandleStream(StreamEvent.WORKFLOW_FINISHED,null, new Result()
                {
                    Data = result
                })));

                // Xóa hết các cache khi đã xử lý xong
                await RemoveCache(data.WorkflowID);
            }
        }

        public async Task RemoveCache(string workflowID)
        {
            await _serviceCached.RemoveCacheAsync(string.Format(CacheString.WorkflowInfo, workflowID));
            await _serviceCached.RemoveCacheAsync(string.Format(CacheString.WorkflowContext, workflowID));
            await _serviceCached.RemoveCacheAsync(string.Format(CacheString.WorkflowDependency, workflowID));
            await _serviceCached.RemoveCacheAsync(string.Format(CacheString.WorkflowCompletedNode, workflowID));
            await _serviceCached.RemoveCacheAsync(string.Format(CacheString.WorkflowNode, workflowID));
        }

        private async Task<object> ExecuteNodeAsync(Node node, string workflowID)
        {
            var inputs = await GetInputForNode(node, workflowID);

            object result = node.Data.Type switch
            {
                "start" => inputs, // Node start thì input như nào output như vậy
                "code" => await ExecuteCodeNodeAsync(node, inputs),
                "end" => await ExecuteEndNodeAsync(node, inputs),
                _ => throw new Exception($"Unknown node type: {node.Data.Type}")
            };

            var context = await _serviceCached.GetCacheAsync<Dictionary<string, Dictionary<string, object>>>(string.Format(CacheString.WorkflowContext, workflowID));

            // Lưu kết quả vào _context để các node sau có thể dùng
            context[node.NodeID] = result is Dictionary<string, object> dict ?
                dict : new Dictionary<string, object> {
                    { "Result", result }
                };

            await _serviceCached.SetCacheAsync(string.Format(CacheString.WorkflowContext, workflowID), context);

            return context;
        }

        /// <summary>
        /// Chuẩn bị tham số đầu vào cho node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, object>> GetInputForNode(Node node, string workflowID)
        {
            var inputs = new Dictionary<string, object>();
            var context = await _serviceCached.GetCacheAsync<Dictionary<string, Dictionary<string, object>>>(string.Format(CacheString.WorkflowContext, workflowID));

            // Node Start thì lấy luôn input từ client làm input đầu vào
            if (node.Data.Type == "start")
            {
                inputs = (Dictionary<string, object>)context[node.NodeID];
            }
            // Node end thì lấy luôn kết quả của node trước làm input đầu vào
            else if (node.Data.Type == "end")
            {
                var workflow = await _serviceCached.GetCacheAsync<Workflow>(string.Format(CacheString.WorkflowInfo, workflowID));
                var nodeIDPre = workflow.Graph.Edges
                    .Where(e => e.Target == node.NodeID) // Tìm các cạnh có đích là node con
                    .Select(e => e.Source) // Lấy ID của node cha
                    .ToList();
                inputs = (Dictionary<string, object>)context[nodeIDPre[0]];
            }
            else
            {
                foreach (var variable in node.Data.Variables)
                {
                    string sourceNodeID = variable.ValueSelector[0];
                    string sourceVar = variable.ValueSelector[1];

                    if (context.ContainsKey(sourceNodeID) && context[sourceNodeID] is Dictionary<string, object> sourceData)
                    {
                        if (sourceData.TryGetValue(sourceVar, out var value))
                        {
                            inputs[variable.Variable] = value;
                        }
                    }
                }
            }

            return inputs;
        }

        private async Task<Dictionary<string, object>> ExecuteCodeNodeAsync(Node node, Dictionary<string, object> inputs)
        {
            // Giả sử node này chạy code JavaScript
            var result = await ExecuteJavaScriptAsync(node, inputs);

            return result;
        }

        private async Task<Dictionary<string, object>> ExecuteJavaScriptAsync(Node node, Dictionary<string, object> inputs)
        {
            var result = new Dictionary<string, object>();
            using var engine = new V8ScriptEngine();

            string declare = "var inputs = { ";
            foreach (var item in inputs)
            {
                declare += $"{item.Key}: {item.Value}, ";
            }
            declare = declare.TrimEnd(',', ' ') + " };"; // Xóa dấu phẩy cuối
            engine.Execute(declare);

            engine.Execute(node.Data.Code);

            var resultScript = engine.Invoke("main", engine.Script.inputs) as ScriptObject;

            var listPropnameJs = resultScript.PropertyNames.ToList();
            if (listPropnameJs.Count != node.Data.Outputs.Count)
            {
                throw new Exception("Not all output parameters are validated.");
            }

            foreach (var item in node.Data.Outputs)
            {
                if (listPropnameJs.Contains(item.Variable))
                {
                    result.Add(item.Variable, resultScript[item.Variable]);
                }
                else
                {
                    throw new Exception($"Ouput {item.Variable} is missing");
                }
            }

            return result;
        }

        private async Task<Dictionary<string, object>> ExecuteEndNodeAsync(Node node, Dictionary<string, object> inputs)
        {
            var result = new Dictionary<string, object>();
            foreach (var item in node.Data.Outputs)
            {
                if (inputs.ContainsKey(item.Variable))
                {
                    result.Add(item.Variable, inputs[item.Variable]);
                }
            }

            return result;
        }

        public object HandleStream(string eventName, string nodeID = null, Result result = null)
        {
            return new
            {
                eventName,
                nodeID,
                result
            };
        }
    }

    public class WorkflowConsumer : BackgroundService
    {

        #region Declaration

        private readonly ISubscriber<WorkflowQueueData> _workflowSubscriber;

        private readonly IRunWorkflowHandler _runWorkflowHandler;

        #endregion

        #region Constructor

        public WorkflowConsumer(IRunWorkflowHandler runWorkflowHandler, ISubscriber<WorkflowQueueData> workflowSubscriber)
        {
            _runWorkflowHandler = runWorkflowHandler;
            _workflowSubscriber = workflowSubscriber;
        }

        #endregion

        #region Method

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _workflowSubscriber.Subscribe(_runWorkflowHandler.Handle, stoppingToken);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {

            }
        }

        #endregion
    }
}
