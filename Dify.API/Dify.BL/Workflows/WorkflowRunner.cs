using Dify.Common.Entities;
using Dify.Common.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript;
using Dify.Common.Model;
//using static Microsoft.ClearScript.V8.V8CpuProfile;

namespace Dify.BL.Workflows
{
    public class WorkflowRunner
    {

        #region Declare

        private readonly Dictionary<string, object> _context = new(); // Lưu trạng thái biến giữa các node
        private readonly Dictionary<string, List<string>> _dependencies = new(); // Lưu các node con của từng node
        private readonly Queue<string> _readyQueue = new(); // Danh sách các node sẵn sàng chạy
        private readonly HashSet<string> _completedNodes = new(); // Các node đã hoàn thành
        private readonly Dictionary<string, Node> _nodeLookup = new();
        public event Action<string, string, object> OnNodeCompleted; // Gửi SSE khi node hoàn thành
        private Dictionary<string, object> _inputs { get; set; } // Input đầu vào từ client
        private readonly Workflow _workflow; // Workflow cần chạy

        #endregion

        #region Constructor

        public WorkflowRunner(Workflow workflow, Dictionary<string, object> inputs)
        {
            _workflow = workflow;
            _inputs = inputs;

            // Tạo lookup cho nhanh
            foreach (var node in workflow.Graph.Nodes)
            {
                _nodeLookup[node.NodeID] = node;
            }

            // Xây dựng dependency graph từ các edges
            foreach (var edge in workflow.Graph.Edges)
            {
                if (!_dependencies.ContainsKey(edge.Source))
                {
                    _dependencies[edge.Source] = new List<string>();
                }
                _dependencies[edge.Source].Add(edge.Target);
            }

            // Thêm node bắt đầu vào hàng chờ
            var startNode = _workflow.Graph.Nodes.FirstOrDefault(n => n.Data.Type == "start");
            if (startNode != null)
            {
                _readyQueue.Enqueue(startNode.NodeID);
                _context.Add(startNode.NodeID, _inputs);
            }
        }

        #endregion

        #region Public Method
        public async Task RunAsync()
        {
            OnNodeCompleted?.Invoke(StreamEvent.WORKFLOW_STARTED, null, null);

            while (_readyQueue.Count > 0)
            {
                var nodeID = _readyQueue.Dequeue();
                var node = _nodeLookup[nodeID];

                OnNodeCompleted?.Invoke(StreamEvent.NODE_STARTED, nodeID, null);
                try
                {
                    object result = await ExecuteNodeAsync(node);
                    _completedNodes.Add(nodeID);
                    OnNodeCompleted?.Invoke(StreamEvent.NODE_FINISHED, nodeID, new Result()
                    {
                        Data = result
                    });
                }
                catch (Exception ex)
                {
                    OnNodeCompleted?.Invoke(StreamEvent.ERROR, nodeID, new Result()
                    {
                        Success = false,
                        Error = $"Node {node.Data.Title} run failed: " + ex.Message
                    });
                }

                // Đưa các node con vào hàng đợi nếu tất cả node cha của nó đã hoàn thành
                if (_dependencies.ContainsKey(nodeID))
                {
                    foreach (var childID in _dependencies[nodeID])
                    {
                        var allParentsCompleted = _workflow.Graph.Edges
                            .Where(e => e.Target == childID)
                            .All(e => _completedNodes.Contains(e.Source));

                        if (allParentsCompleted)
                        {
                            _readyQueue.Enqueue(childID);
                        }
                    }
                }
            }

            OnNodeCompleted?.Invoke("WorkflowEnd", null, new Result()
            {
                Data = _context
            });
        }

        #endregion

        #region private method

        private async Task<object> ExecuteNodeAsync(Node node)
        {
            var inputs = GetInputForNode(node);

            object result = node.Data.Type switch
            {
                "start" => inputs, // Node start thì input như nào output như vậy
                "code" => await ExecuteCodeNodeAsync(node, inputs),
                "end" => await ExecuteEndNodeAsync(node, inputs),
                _ => throw new Exception($"Unknown node type: {node.Data.Type}")
            };

            // Lưu kết quả vào _context để các node sau có thể dùng
            _context[node.NodeID] = result is Dictionary<string, object> dict ? 
                dict : new Dictionary<string, object> { 
                    { "Result", result } 
                };

            return _context;
        }

        /// <summary>
        /// Chuẩn bị tham số đầu vào cho node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Dictionary<string, object> GetInputForNode(Node node)
        {
            var inputs = new Dictionary<string, object>();

            // Node Start thì lấy luôn input từ client làm input đầu vào
            if (node.Data.Type == "start")
            {
                inputs = _inputs;
            }
            // Node end thì lấy luôn kết quả của node trước làm input đầu vào
            else if (node.Data.Type == "end")
            {
                var nodeIDPre = _workflow.Graph.Edges
                    .Where(e => e.Target == node.NodeID) // Tìm các cạnh có đích là node con
                    .Select(e => e.Source) // Lấy ID của node cha
                    .ToList();
                inputs = (Dictionary<string, object>)_context[nodeIDPre[0]];
            }
            else
            {
                foreach (var variable in node.Data.Variables)
                {
                    string sourceNodeID = variable.ValueSelector[0];
                    string sourceVar = variable.ValueSelector[1];

                    if (_context.ContainsKey(sourceNodeID) && _context[sourceNodeID] is Dictionary<string, object> sourceData)
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
        #endregion

    }
}
