using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Entities
{
    public class Workflow
    {
        public Guid? WorkflowID { get; set; }

        public string? Name { get; set; }

        public Graph? Graph { get; set; }

        public string? Version { get; set; }
    }

    public class Graph
    {
        public List<Node> Nodes { get; set; }

        public List<Edge> Edges { get; set; }

        public ViewPort ViewPort { get; set; }
    }

    public class ViewPort
    {
        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Zoom { get; set; }
    }

    #region Node

    public class Node
    {
        public string NodeID { get; set; }

        public DataNode Data { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public Position Position { get; set; }

        public Position PositionAbsolute { get; set; }

        public string? SourcePosition { get; set; }

        public string? TargetPosition { get; set; }

        public string? Type { get; set; }

        public bool? Selected { get; set; }
    }

    public class DataNode
    {
        public string Title { get; set; }

        public bool? Selected { get; set; }

        public string? Desc { get; set; }

        public string Type { get; set; }

        public List<VariableEntity>? Variables { get; set; }

        public string? CodeLanguage { get; set; }

        public string? Code { get; set; }

        public List<NodeOutputs>? Outputs { get; set; }

    }

    public class NodeOutputs
    {
        public string Variable { get; set; }
        public string? Type { get; set; }
        public List<string>? ValueSelector { get; set; }
    }

    public class VariableEntity
    {
        public string Variable { get; set; }

        public string? Label { get; set; }

        public string Type { get; set; }

        public string? MaxLength { get; set; }

        public bool Required { get; set; } = true;

        public List<string>? Options { get; set; } = new List<string>();

        public List<string> ValueSelector { get; set; } = new List<string>();
    }

    #endregion

    #region Dùng chung
    public class Position
    {
        public decimal X { get; set; }

        public decimal Y { get; set; }
    }

    #endregion

    #region Edge
    public class Edge
    {
        public string EdgeID { get; set; }

        public string? Type { get; set; }

        public string? Source { get; set; }

        public string? SourceHandle { get; set; }

        public string? Target { get; set; }

        public string? TargetHandle { get; set; }

        public DataEdge Data { get; set; }

        public int ZIndex { get; set; }
    }

    public class DataEdge
    {
        public string? SourceType { get; set; }

        public string? TargetType { get; set; }

        public bool IsInIteration { get; set; }
    }
    #endregion
}
