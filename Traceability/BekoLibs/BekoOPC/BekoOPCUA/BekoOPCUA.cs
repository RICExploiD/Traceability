using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageTypeLib;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace BekoOPCUA
{
    /// <summary>
    /// Type of encription for certificate.
    /// </summary>
    public enum SHAType
    {
        SHA1,
        SHA2
    }
    public class BekoOPCUA : IBekoOPCUA
    {
        /// <summary>
        /// OPC session.
        /// </summary>
        private ISession _session = null;

        /// <summary>
        /// Stores configuration for OPC session and application.
        /// </summary>
        private ApplicationConfiguration _config;

        /// <summary>
        /// Client application for OPC session.
        /// </summary>
        private ApplicationInstance _application;
        private readonly UserIdentity _userIdentity;

        /// <summary>
        /// OPC address in format IP:PORT.
        /// </summary>
        private readonly string _endPoint;

        // Subscription management
        /// <summary>
        /// List of all active subscriptions.
        /// </summary>
        private List<Subscription> _subscriptions = new List<Subscription>();
        /// <summary>
        /// Dictionary of all subscribed nodes.
        /// </summary>
        private Dictionary<string, MonitoredItem> _subscribedNodes = new Dictionary<string, MonitoredItem>();

        // Reconnection handlers
        private SessionReconnectHandler m_reconnectHandler;
        private ReverseConnectManager m_reverseConnectManager = null;

        // Name of application that will be used in certificate
        private string _appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

        /// <summary>
        /// Seconds between reconnection attempts.
        /// </summary>
        public int ReconnectPeriod { get; set; } = 2;
        /// <summary>
        /// Connection status
        /// </summary>
        public bool IsConnected { get; private set; } = false;
        /// <summary>
        /// Access status
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        #region OPC UA Events

        /// <summary>
        /// Method that will be fired on opc connection status changed.
        /// </summary>
        public event delegOnConnection OnConnectionChange;
        public delegate void delegOnConnection(bool IsConnected);

        /// <summary>
        /// Method that will be fiered on log writing.
        /// </summary>
        public event delegHandlerStrType OnLogEvent;
        public delegate void delegHandlerStrType(string msg, MessageType type);
        #endregion

        /// <summary>
        /// Create an instance of OPC UA written on .Net Framework 4.8.
        /// </summary>
        /// <param name="endPoint">
        /// End point for incoming connection in format IP:Port.
        /// </param>
        /// <param name="certificateType">
        /// What king of encryption to use on certificate generation.
        /// </param>
        /// <param name="userName">
        /// User name to login to the server. 
        /// Nothing for Anonymus connection.
        /// </param>
        /// <param name="userPassword">
        /// Password to login to the server. 
        /// Nothing for Anonymus connection.
        /// </param>
        public BekoOPCUA(string endPoint, SHAType certificateType, string userName = null, string userPassword = null)
        {
            _endPoint = $"opc.tcp://{endPoint}";
            _userIdentity = GetIdentity(userName, userPassword);
            CreateConfig(certificateType);
            CreateOpcApplication(certificateType);
        }
        ~BekoOPCUA()
        {
            Terminate();
        }

        /// <summary>
        /// Read value of specified node.
        /// </summary>
        /// <param name="nodeId">
        /// Id of the node to read. 
        /// </param>
        /// <returns>An object stored in the Node</returns>
        public object Read(string nodeId)
        {
            var value = _session.ReadValue(nodeId);
            return value.WrappedValue.Value;
        }

        /// <summary>
        /// Write Byte value to specified node.
        /// </summary>
        /// <param name="nodeId">Id of the node to write to.</param>
        /// <param name="value">Byte value to write.</param>
        public void WriteByte(string nodeId, byte value)
        {
            Write(nodeId, (byte)value);
        }

        /// <summary>
        /// Write any necessery value to specified node.
        /// </summary>
        /// <param name="nodeId">Id of the node to write to.</param>
        /// <param name="value">Value to write.</param>
        public void Write(string nodeId, object value)
        {
            if (IsReadOnly)
            {
                OnLogEvent?.Invoke("Attempt to write failed. OPC is Readonly.", MessageType.Warning);
                return;
            }

            WriteValue nodeToWrite = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue { WrappedValue = new Variant(value) }
            };
            var nodesToWrite = new WriteValueCollection { nodeToWrite };

            // read the attributes.
            StatusCodeCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            ResponseHeader responseHeader = _session.Write(
                null,
                nodesToWrite,
                out results,
                out diagnosticInfos
                );

            ClientBase.ValidateResponse(results, nodesToWrite);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToWrite);
            // check for error.
            if (StatusCode.IsBad(results[0]))
            {
                OnLogEvent?.Invoke($"Error during writing to node {nodeId}. OPC Status code: {results[0]}.", MessageType.Error);
                throw ServiceResultException.Create(results[0], 0, diagnosticInfos, responseHeader.StringTable);
            }
            else
                OnLogEvent?.Invoke(
                    $"Wrote {value} of type {value.GetType().Name} to Node {nodeId}",
                    MessageType.Info);
        }

        /// <summary>
        /// Creates new subscription and subscribes specified nod to it.
        /// </summary>
        /// <param name="nodeId">Id of the node to subscribe</param>
        /// <param name="publishingInterval">
        /// How fast server will responce to node change in ms.
        /// </param>
        /// <returns>Subscribed item. If no such item exists returns Null.</returns>
        public MonitoredItem SubscribeAndGetTag(string nodeId, int publishingInterval = 100)
        {
            Subscribe(nodeId, publishingInterval);

            return GetSubscribedTag(nodeId);
        }

        /// <summary>
        /// Creates new subscription and subscribes specified nod to it.
        /// </summary>
        /// <param name="nodeId">Id of the node to subscribe</param>
        /// <param name="publishingInterval">
        /// How fast server will responce to node change in ms.
        /// </param>
        public void Subscribe(string nodeId, int publishingInterval = 100)
        {
            if (_session == null)
                return;
            try
            {
                var newSubscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = publishingInterval,
                };
                _subscriptions.Add(newSubscription);

                var tagToSub = new MonitoredItem(newSubscription.DefaultItem) { DisplayName = $"Node {nodeId}", StartNodeId = nodeId };
                try
                {
                    _subscribedNodes.Add(nodeId, tagToSub);
                }
                catch (ArgumentException)
                {
                    OnLogEvent?.Invoke($"Node {nodeId} already subscribed", MessageType.Warning);
                    _subscriptions.Remove(newSubscription);
                    return;
                }
                newSubscription.AddItem(tagToSub);

                _session.AddSubscription(newSubscription);
                newSubscription.Create();
            }
            catch(Exception ex)
            {
                OnLogEvent.Invoke(
                    $"Error on subsription process. Tried subscribe node {nodeId}. Error: {ex}", 
                    MessageType.Error);
            }
        }

        /// <summary>
        /// Returns a specified subscribed node. 
        /// </summary>
        /// <param name="nodeId">Id of subscribed node.</param>
        /// <returns>Subscribed item. If no such item exists returns Null.</returns>
        public MonitoredItem GetSubscribedTag(string nodeId)
        {
            if (!_subscribedNodes.TryGetValue(nodeId, out MonitoredItem subbedNode))
            {
                OnLogEvent?.Invoke($"No such Node \"{nodeId}\" in subscriptions.", MessageType.Warning);
                return null;
            }

            return subbedNode;
        }

        /// <summary>
        /// Check if specified nodes exists and accessible.
        /// </summary>
        /// <param name="nodeIds">List of nodes ids to check.</param>
        /// <param name="nodes">List of existing nodes.</param>
        /// <param name="falseNodeId">Node which is not exists or not accessible</param>
        /// <returns>State of existance</returns>
        public bool IsNodesExist(IList<string> nodeIds, IList<INode> nodes, out string falseNodeId)
        {
            foreach (string nodeId in nodeIds)
            {
                if (!IsNodeExist(nodeId, nodes))
                {
                    falseNodeId = nodeId;
                    return false;
                }
            }

            falseNodeId = null;
            return true;
        }

        /// <summary>
        /// Check if specified node exists and accessible.
        /// </summary>
        /// <param name="nodeId">Node id to check</param>
        /// <param name="nodes">List of existing nodes</param>
        /// <returns>State of existance</returns>
        public bool IsNodeExist(string nodeId, IList<INode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.NodeId.ToString().Equals(nodeId))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Search for all accessible Object nodes on the server.
        /// </summary>
        /// <returns>List of all accessible Object nodes.</returns>
        public IList<INode> BrowseAllObjectNodes()
        {
            var result = new List<INode>();
            result.AddRange(BrowseAllNodes().Where(n => n is ObjectNode));

            return result;
        }

        /// <summary>
        /// Search for all accessible Type nodes on the server.
        /// </summary>
        /// <returns>List of all accessible Type nodes.</returns>
        public IList<INode> BrowseAllTypeNodes()
        {
            var result = new List<INode>();
            result.AddRange(BrowseAllNodes().Where(n => n is TypeNode));

            return result;
        }

        /// <summary>
        /// Search for all accessible Variable nodes on the server.
        /// </summary>
        /// <returns>List of all accessible Variable nodes.</returns>
        public IList<INode> BrowseAllVariableNodes()
        {
            var result = new List<INode>();
            result.AddRange(BrowseAllNodes().Where(n => n is VariableNode));

            return result;
        }

        /// <summary>
        /// Search for all accessible nodes on the server.
        /// </summary>
        /// <returns>List of all accessible nodes.</returns>
        public IList<INode> BrowseAllNodes()
        {
            var result = new List<INode>();
            var nodesToBrowse = new ExpandedNodeIdCollection {
                ObjectIds.ObjectsFolder
            };

            while (nodesToBrowse.Count > 0)
            {
                var nextNodesToBrowse = new ExpandedNodeIdCollection();
                foreach (var node in nodesToBrowse)
                {
                    try
                    {
                        var organizers = _session.NodeCache.FindReferences(
                            node,
                            ReferenceTypeIds.Organizes,
                            false,
                            false);
                        var components = _session.NodeCache.FindReferences(
                            node,
                            ReferenceTypeIds.HasComponent,
                            false,
                            false);
                        var properties = _session.NodeCache.FindReferences(
                            node,
                            ReferenceTypeIds.HasProperty,
                            false,
                            false);
                        nextNodesToBrowse
                            .AddRange(organizers
                            .Where(n => n is ObjectNode)
                            .Select(n => n.NodeId)
                            .ToList());
                        nextNodesToBrowse
                            .AddRange(components
                            .Where(n => n is ObjectNode)
                            .Select(n => n.NodeId)
                            .ToList());

                        result.AddRange(organizers);
                        result.AddRange(components);
                        result.AddRange(properties);
                    }
                    catch (ServiceResultException sre)
                    {
                        if (sre.StatusCode == StatusCodes.BadUserAccessDenied)
                        {
                            OnLogEvent?.Invoke($"Access denied during the search: Skip node {node}, {node.Identifier}.",
                                MessageType.Warning);
                        }
                    }
                    catch(NullReferenceException)
                    {
                        OnLogEvent?.Invoke($"Session is not created or exists.", MessageType.Error);
                    }
                }
                nodesToBrowse = nextNodesToBrowse;
            }
            return result;
        }

        /// <summary>
        /// Create a session and connects to OPC server.
        /// </summary>
        public void Run()
        {
            if (_session == null || !_session.Connected)
            {
                ConfiguredEndpoint configuredEndpoint;
                try
                {
                    var selectedEndPoint =
                        CoreClientUtils.SelectEndpoint(_endPoint, useSecurity: false);
                    configuredEndpoint =
                        new ConfiguredEndpoint(null, selectedEndPoint, EndpointConfiguration.Create(_config));
                }
                catch(Exception ex)
                {
                    OnLogEvent.Invoke($"Error on End Point configuration. {ex}",
                        MessageType.Error);
                    return;
                }

                try
                {
                    _session = Session.Create
                        (_config,
                        configuredEndpoint,
                        false,
                        "",
                        600000, // Session time out
                        _userIdentity,
                        null)
                        .GetAwaiter()
                        .GetResult();

                    _session.KeepAlive += SessionReconnect;
                    ConnectionStatusChange(_session.Connected);
                }
                catch (Exception ex)
                {
                    OnLogEvent?.Invoke($"Unable to connect to the server. Error: {ex}", 
                        MessageType.Error);
                }
            }
        }

        /// <summary>
        /// Terminates the current session.
        /// </summary>
        public void Terminate()
        {
            if (_session != null && _session.Connected)
            {
                _session.Close();
                ConnectionStatusChange(_session.Connected);
                OnLogEvent?.Invoke("Session was terminated by method.", MessageType.Warning);
            }
        }

        private void SessionReconnect(ISession sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                ConnectionStatusChange(false);
                OnLogEvent?.Invoke($"{e.Status}, {sender.OutstandingRequestCount}/{sender.DefunctRequestCount}", MessageType.Warning);
                if (m_reconnectHandler == null)
                {
                    OnLogEvent?.Invoke("Reconnecting.", MessageType.Warning);
                    m_reconnectHandler = new SessionReconnectHandler();
                    if (m_reverseConnectManager != null)
                    {
                        m_reconnectHandler.BeginReconnect(sender, m_reverseConnectManager, ReconnectPeriod * 1000, Client_ReconnectComplete);
                    }
                    else
                    {
                        m_reconnectHandler.BeginReconnect(sender, ReconnectPeriod * 1000, Client_ReconnectComplete);
                    }
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, m_reconnectHandler))
            {
                return;
            }

            _session = m_reconnectHandler.Session;
            m_reconnectHandler.Dispose();
            m_reconnectHandler = null;

            ConnectionStatusChange(_session.Connected);
            OnLogEvent?.Invoke("Reconnected", MessageType.Success);
        }

        private void CreateOpcApplication(SHAType type)
        {
            ushort minKeySize;
            switch (type)
            {
                case SHAType.SHA1:
                    minKeySize = 1024;
                    break;
                case SHAType.SHA2:
                    minKeySize = 2048;
                    break;
                default:
                    minKeySize = 1024;
                    break;
            }
            _application = new ApplicationInstance
            {
                ApplicationName = _appName,
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = _config
            };

            try
            {
                _application // Check if certificate is valid
                    .CheckApplicationInstanceCertificate(false, minKeySize)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                OnLogEvent.Invoke($"Error during certificate validation {ex}",
                    MessageType.Error);
            }
        }

        private void CreateConfig(SHAType certificateType)
        {
            bool suppressNonceValidationErrors = true;
            bool rejectSHA1SignedCertificates = false;
            ushort minimumCertificateKeySize = 1024;
            switch (certificateType)
            {
                case SHAType.SHA1:
                    suppressNonceValidationErrors = true;
                    rejectSHA1SignedCertificates = false;
                    minimumCertificateKeySize = 1024;
                    break;
                case SHAType.SHA2:
                    suppressNonceValidationErrors = false;
                    rejectSHA1SignedCertificates = true;
                    minimumCertificateKeySize = 2048;
                    break;
            }

            // Create a config for app
            _config = new ApplicationConfiguration
            {
                ApplicationName = _appName,
                ApplicationUri = Opc.Ua.Utils.Format($"urn:{System.Net.Dns.GetHostName()}:{_appName}"),
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = @"Directory",
                        StorePath = @"./CertificateStores\MachineDefault",
                        SubjectName = _appName
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = @"Directory",
                        StorePath = @"./CertificateStores\UA Certificate Authorities"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = @"Directory",
                        StorePath = @"./CertificateStores\UA Applications"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = @"Directory",
                        StorePath = @"./CertificateStores\RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    SuppressNonceValidationErrors = suppressNonceValidationErrors,
                    RejectSHA1SignedCertificates = rejectSHA1SignedCertificates,
                    MinimumCertificateKeySize = minimumCertificateKeySize
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };
            try
            {
                _config
                .Validate(ApplicationType.Client)
                .GetAwaiter()
                .GetResult();
            }
            catch (NullReferenceException ex)
            {
                OnLogEvent?.Invoke($"Config was not created in OPC UA -> {ex.Message}. Stack trace: {ex};",
                    MessageType.Error);
            }
            catch (TaskCanceledException ex)
            {
                OnLogEvent.Invoke($"Config validation was canceled -> {ex.Message}. Stack trace: {ex}",
                    MessageType.Error);
            }
            catch (Exception ex)
            {
                OnLogEvent.Invoke($"Unexpected error occure -> {ex.Message}. Stack trace: {ex}",
                    MessageType.Error);
            }

            if (_config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                _config.CertificateValidator.CertificateValidation += (s, e) =>
                    {
                        e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
                    };
            }
        }

        private UserIdentity GetIdentity(string userName, string userPassword)
        {
            return
                (userName != null) && (userPassword != null) ?
                new UserIdentity(userName, userPassword) :
                new UserIdentity(new AnonymousIdentityToken());
        }

        private void ConnectionStatusChange(bool NewIsConnected)
        {
            if (IsConnected != NewIsConnected)
            {
                IsConnected = NewIsConnected;
                OnConnectionChange?.Invoke(NewIsConnected);
                if (NewIsConnected)
                {
                    OnLogEvent?.Invoke("Connection with OPC UA established.", MessageType.Success);
                }
                else
                {
                    OnLogEvent?.Invoke("Connection with OPC UA lost.", MessageType.Error);
                }
            }
        }
    }
}
