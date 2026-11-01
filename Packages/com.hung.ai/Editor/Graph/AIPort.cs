using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    /// <summary>Vertical port (parent above, children below) whose edge connector reports drops on empty space.</summary>
    public sealed class AIPort : Port
    {
        AIPort(Direction direction, Capacity capacity) : base(Orientation.Vertical, direction, capacity, typeof(AINode)) { }

        public static AIPort Create(Direction direction, Capacity capacity, IEdgeConnectorListener listener)
        {
            var port = new AIPort(direction, capacity) { portName = string.Empty };
            port.m_EdgeConnector = new EdgeConnector<AIEdge>(listener);
            port.AddManipulator(port.m_EdgeConnector);
            port.AddToClassList("ai-port");
            return port;
        }
    }

    /// <summary>Edge drawn as the mockup's vertical S-curve.</summary>
    public sealed class AIEdge : Edge
    {
        protected override EdgeControl CreateEdgeControl() => new AIEdgeControl { capRadius = 4f, interceptWidth = 6f };
    }

    /// <summary>Bends the edge at half height: down from the parent, across, then down into the child.</summary>
    public sealed class AIEdgeControl : EdgeControl
    {
        protected override void ComputeControlPoints()
        {
            base.ComputeControlPoints();
            Vector2[] points = controlPoints;
            if (points != null && points.Length >= 4) ApplyVerticalS(points);
        }

        /// <summary>points[0] is the parent end and points[3] the child end; the two middle control points move to the midpoint height.</summary>
        public static void ApplyVerticalS(Vector2[] points)
        {
            float midY = (points[0].y + points[3].y) * 0.5f;
            points[1] = new Vector2(points[0].x, midY);
            points[2] = new Vector2(points[3].x, midY);
        }
    }

    /// <summary>GraphView's default drop behaviour, plus: dropping an edge on empty space opens the node search.</summary>
    internal sealed class AIEdgeConnectorListener : IEdgeConnectorListener
    {
        readonly AIGraphView view;

        public AIEdgeConnectorListener(AIGraphView view)
        {
            this.view = view;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Port from = edge.output != null ? edge.output : edge.input;
            if (from != null) view.OpenSearchFromPort(from, position);
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            var stale = new List<GraphElement>();
            if (edge.input.capacity == Port.Capacity.Single)
                foreach (Edge existing in edge.input.connections)
                    if (existing != edge) stale.Add(existing);
            if (edge.output.capacity == Port.Capacity.Single)
                foreach (Edge existing in edge.output.connections)
                    if (existing != edge) stale.Add(existing);
            if (stale.Count > 0) graphView.DeleteElements(stale);
            var change = new GraphViewChange { edgesToCreate = new List<Edge> { edge } };
            if (graphView.graphViewChanged != null) change = graphView.graphViewChanged(change);
            foreach (Edge created in change.edgesToCreate)
            {
                graphView.AddElement(created);
                created.input.Connect(created);
                created.output.Connect(created);
            }
        }
    }
}
