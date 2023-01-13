using Opc.Ua;
using Opc.Ua.Client;
using System.Collections.Generic;

namespace BekoOPCUA
{
    public interface IBekoOPCUA
    {
        bool IsConnected { get; }
        int ReconnectPeriod { get; set; }
        bool IsReadOnly { get; set;}

        event BekoOPCUA.delegOnConnection OnConnectionChange;
        event BekoOPCUA.delegHandlerStrType OnLogEvent;

        IList<INode> BrowseAllNodes();
        IList<INode> BrowseAllObjectNodes();
        IList<INode> BrowseAllTypeNodes();
        IList<INode> BrowseAllVariableNodes();
        void Run();
        void Terminate();
        void Subscribe(string nodeId, int publishingInterval = 100);
        void WriteByte(string nodeId, byte value);
        void Write(string nodeId, object value);
        object Read(string nodeId);
        bool IsNodeExist(string nodeId, IList<INode> nodes);
        bool IsNodesExist(IList<string> nodeIds, IList<INode> nodes, out string falseOnNodeId);
        MonitoredItem GetSubscribedTag(string nodeId);
        MonitoredItem SubscribeAndGetTag(string nodeId, int publishingInterval = 100);
    }
}