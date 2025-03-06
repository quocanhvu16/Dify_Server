namespace Dify.Common.Constant
{
    public static class CacheString
    {
        public const string WorkflowInfo = "workflow:{0}:info"; // Lưu dữ liệu workflow (Workflow)
        public const string WorkflowContext = "workflow:{0}:context"; // Lưu context của workflow (Dictionary<string, object>)
        public const string WorkflowDependency = "workflow:{0}:dependency"; // Lưu các phụ thuộc của workflow (Dictionary<string, List<string>>)
        public const string WorkflowCompletedNode = "workflow:{0}:completednode"; // Lưu các node đã hoàn thành của workflow (HashSet<string>)
        public const string WorkflowNode = "workflow:{0}:node"; // Lưu dữ liệu các node (Dictionary<string, Node>)
    }
}
