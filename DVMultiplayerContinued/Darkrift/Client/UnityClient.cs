﻿using DarkRift.Dispatching;
using DVMultiplayer;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.Serialization;

namespace DarkRift.Client.Unity
{
    [AddComponentMenu("DarkRift/Client")]
    public sealed class UnityClient : SingletonBehaviour<UnityClient>
    {
        /// <summary>
        ///     The IP address this client connects to.
        /// </summary>
        /// <remarks>
        ///     If <see cref="Host"/> is not an IP address this property will perform a DNS resolution which may be slow!
        /// </remarks>
        public IPAddress Address
        {
            get { return Dns.GetHostAddresses(host)[0]; }
            set { host = value.ToString(); }
        }

        /// <summary>
        ///     The host this client connects to.
        /// </summary>
        public string Host
        {
            get { return host; }
            set { host = value; }
        }

        [SerializeField]
        [FormerlySerializedAs("address")]
        [Tooltip("The host to connect to.")]
        private string host = "localhost";

        /// <summary>
        ///     The port this client connects to.
        /// </summary>
        public ushort Port
        {
            get { return port; }
            set { port = value; }
        }

        [SerializeField]
        [Tooltip("The port on the server the client will connect to.")]
        private ushort port = 4296;

        [SerializeField]
        [Tooltip("Whether to disable Nagel's algorithm or not.")]
#pragma warning disable IDE0044 // Add readonly modifier, Unity can't serialize readonly fields
        private bool noDelay = false;

        [SerializeField]
        [Tooltip("Indicates whether the client will connect to the server in the Start method.")]
        private bool autoConnect = false;

        [SerializeField]
        [Tooltip("Specifies that DarkRift should take care of multithreading and invoke all events from Unity's main thread.")]
        private volatile bool invokeFromDispatcher = true;

        [SerializeField]
        [Tooltip("Specifies whether DarkRift should log all data to the console.")]
        private volatile bool sniffData = false;
#pragma warning restore IDE0044 // Add readonly modifier
        #region Cache settings
        #region Legacy
        /// <summary>
        ///     The maximum number of <see cref="DarkRiftWriter"/> instances stored per thread.
        /// </summary>
        [Obsolete("Use the ObjectCacheSettings property instead.")]
        public int MaxCachedWriters
        {
            get
            {
                return ObjectCacheSettings.MaxWriters;
            }
        }

        /// <summary>
        ///     The maximum number of <see cref="DarkRiftReader"/> instances stored per thread.
        /// </summary>
        [Obsolete("Use the ObjectCacheSettings property instead.")]
        public int MaxCachedReaders
        {
            get
            {
                return ObjectCacheSettings.MaxReaders;
            }
        }

        /// <summary>
        ///     The maximum number of <see cref="Message"/> instances stored per thread.
        /// </summary>
        [Obsolete("Use the ObjectCacheSettings property instead.")]
        public int MaxCachedMessages
        {
            get
            {
                return ObjectCacheSettings.MaxMessages;
            }
        }

        /// <summary>
        ///     The maximum number of <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> instances stored per thread.
        /// </summary>
        [Obsolete("Use the ObjectCacheSettings property instead.")]
        public int MaxCachedSocketAsyncEventArgs
        {
            get
            {
                return ObjectCacheSettings.MaxSocketAsyncEventArgs;
            }
        }

        /// <summary>
        ///     The maximum number of <see cref="ActionDispatcherTask"/> instances stored per thread.
        /// </summary>
        [Obsolete("Use the ObjectCacheSettings property instead.")]
        public int MaxCachedActionDispatcherTasks
        {
            get
            {
                return ObjectCacheSettings.MaxActionDispatcherTasks;
            }
        }
        #endregion Legacy

        /// <summary>
        ///     The client object cache settings in use.
        /// </summary>
        public ClientObjectCacheSettings ClientObjectCacheSettings
        {
            get
            {
#pragma warning disable CS0618 // Type or member is obsolete
                return ObjectCacheSettings as ClientObjectCacheSettings;
            }
            set
            {
                ObjectCacheSettings = value;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        ///     The object cache settings in use.
        /// </summary>
        [Obsolete("Use ClientObjectCacheSettings instead.")]
        public ObjectCacheSettings ObjectCacheSettings { get; set; }

        /// <summary>
        ///     Serialisable version of the object cache settings for Unity.
        /// </summary>
        [SerializeField]
#pragma warning disable IDE0044 // Add readonly modifier, Unity can't serialize readonly fields
        private SerializableObjectCacheSettings objectCacheSettings = new SerializableObjectCacheSettings();
#pragma warning restore IDE0044 // Add readonly modifier, Unity can't serialize readonly fields
        #endregion

        /// <summary>
        ///     Event fired when a message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Event fired when we disconnect form the server.
        /// </summary>
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     The ID the client has been assigned.
        /// </summary>
        public ushort ID
        {
            get
            {
                return Client.ID;
            }
        }

        /// <summary>
        ///     Returns whether or not this client is connected to the server.
        /// </summary>
        [Obsolete("User ConnectionState instead.")]
        public bool Connected
        {
            get
            {
                return Client.Connected;
            }
        }


        /// <summary>
        ///     Returns the state of the connection with the server.
        /// </summary>
        public ConnectionState ConnectionState
        {
            get
            {
                return Client.ConnectionState;
            }
        }

        /// <summary>
        /// 	The actual client connecting to the server.
        /// </summary>
        /// <value>The client.</value>
        public DarkRiftClient Client { get; private set; }

        /// <summary>
        ///     The dispatcher for moving work to the main thread.
        /// </summary>
        public Dispatcher Dispatcher { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public void Initialize()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ObjectCacheSettings = objectCacheSettings.ToClientObjectCacheSettings();

            Client = new DarkRiftClient(ObjectCacheSettings);
#pragma warning restore CS0618 // Type or member is obsolete

            //Setup dispatcher
            Dispatcher = new Dispatcher(true);

            //Setup routing for events
            Client.MessageReceived += Client_MessageReceived;
            Client.Disconnected += Client_Disconnected;
        }

        private void Start()
        {
            //If auto connect is true then connect to the server
            if (autoConnect)
                Connect(host, port, noDelay);
        }

        private void Update()
        {
            //Execute all the queued dispatcher tasks
            Dispatcher.ExecuteDispatcherTasks   ();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //Remove resources
            Close();
        }

        private void OnApplicationQuit()
        {
            //Remove resources
            Close();
        }

        /// <summary>
        ///     Connects to a remote server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        [Obsolete("Use other Connect overloads that automatically detect the IP version.")]
        public void Connect(IPAddress ip, int port, IPVersion ipVersion)
        {
            Client.Connect(ip, port, ipVersion);

            LogConnectionStatus(ip, port.ToString());
        }

        /// <summary>
        ///     Connects to a remote server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        public void Connect(IPAddress ip, int port, bool noDelay)
        {
            Client.Connect(ip, port, noDelay);

            LogConnectionStatus(ip, port.ToString());
        }

        /// <summary>
        ///     Connects to a remote server.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        public void Connect(string host, int port, bool noDelay)
        {
            Connect(Dns.GetHostAddresses(host)[0], port, noDelay);
        }

        /// <summary>
        ///     Connects to a remote server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="tcpPort">The port the server is listening on for TCP.</param>
        /// <param name="udpPort">The port the server is listening on for UDP.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        public void Connect(IPAddress ip, int tcpPort, int udpPort, bool noDelay)
        {
            Client.Connect(ip, tcpPort, udpPort, noDelay);
            LogConnectionStatus(ip, $"{tcpPort} and {udpPort}");
        }

        /// <summary>
        ///     Connects to a remote server.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="tcpPort">The port the server is listening on for TCP.</param>
        /// <param name="udpPort">The port the server is listening on for UDP.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        public void Connect(string host, int tcpPort, int udpPort, bool noDelay)
        {
            Connect(Dns.GetHostAddresses(host)[0], tcpPort, udpPort, noDelay);
        }

        private void LogConnectionStatus(IPAddress ip, string ports)
        {
            if (ConnectionState == ConnectionState.Connected)
                Main.mod.Logger.Log($"[CLIENT] Connected to {ip} on port(s) {ports}.");
            else
                Main.mod.Logger.Log($"[CLIENT] Connection failed to {ip} on port(s) {ports}.");
        }

        /// <summary>
        ///     Connects to a remote asynchronously.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        /// <param name="callback">The callback to make when the connection attempt completes.</param>
        public void ConnectInBackground(IPAddress ip, int port, bool noDelay, DarkRiftClient.ConnectCompleteHandler callback = null)
        {
            if (Client == null)
                Initialize();

            Client.ConnectInBackground(
                ip,
                port,
                noDelay,
                delegate (Exception e)
                {
                    if (callback != null)
                    {
                        if (invokeFromDispatcher)
                            Dispatcher.InvokeAsync(() => callback(e));
                        else
                            callback.Invoke(e);
                    }

                    LogConnectionStatus(ip, port.ToString());
                }
            );
        }

        /// <summary>
        ///     Connects to a remote asynchronously.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        /// <param name="callback">The callback to make when the connection attempt completes.</param>
        public void ConnectInBackground(string host, int port, bool noDelay, DarkRiftClient.ConnectCompleteHandler callback = null)
        {
            ConnectInBackground(
                Dns.GetHostAddresses(host)[0],
                port,
                noDelay,
                callback
            );
        }

        /// <summary>
        ///     Connects to a remote asynchronously.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="tcpPort">The port the server is listening on for TCP.</param>
        /// <param name="udpPort">The port the server is listening on for UDP.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        /// <param name="callback">The callback to make when the connection attempt completes.</param>
        public void ConnectInBackground(IPAddress ip, int tcpPort, int udpPort, bool noDelay, DarkRiftClient.ConnectCompleteHandler callback = null)
        {
            Client.ConnectInBackground(
                ip,
                tcpPort,
                udpPort,
                noDelay,
                delegate (Exception e)
                {
                    if (callback != null)
                    {
                        if (invokeFromDispatcher)
                            Dispatcher.InvokeAsync(() => callback(e));
                        else
                            callback.Invoke(e);
                    }

                    LogConnectionStatus(ip, $"{tcpPort} and {udpPort}");
                }
            );
        }

        /// <summary>
        ///     Connects to a remote asynchronously.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="tcpPort">The port the server is listening on for TCP.</param>
        /// <param name="udpPort">The port the server is listening on for UDP.</param>
        /// <param name="noDelay">Whether to disable Nagel's algorithm or not.</param>
        /// <param name="callback">The callback to make when the connection attempt completes.</param>
        public void ConnectInBackground(string host, int tcpPort, int udpPort, bool noDelay, DarkRiftClient.ConnectCompleteHandler callback = null)
        {
            ConnectInBackground(
                Dns.GetHostAddresses(host)[0],
                tcpPort,
                udpPort,
                noDelay,
                callback
            );
        }

        /// <summary>
        ///     Sends a message to the server.
        /// </summary>
        /// <param name="message">The message template to send.</param>
        /// <returns>Whether the send was successful.</returns>
        public bool SendMessage(Message message, SendMode sendMode)
        {
            if (Client == null || Client.ConnectionState == ConnectionState.Disconnected || Client.ConnectionState == ConnectionState.Disconnecting)
                return false;
            return Client.SendMessage(message, sendMode);
        }

        /// <summary>
        ///     Invoked when DarkRift receives a message from the server.
        /// </summary>
        /// <param name="sender">The client that received the message.</param>
        /// <param name="e">The arguments for the event.</param>
        private void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //If we're handling multithreading then pass the event to the dispatcher
            if (invokeFromDispatcher)
            {
                if (sniffData)
                    Main.Log("Message Received: Tag = " + e.Tag + ", SendMode = " + e.SendMode);

                // DarkRift will recycle the message inside the event args when this method exits so make a copy now that we control the lifecycle of!
                Message message = e.GetMessage();
                MessageReceivedEventArgs args = MessageReceivedEventArgs.Create(message, e.SendMode);

                Dispatcher.InvokeAsync(
                    () =>
                        {
                            EventHandler<MessageReceivedEventArgs> handler = MessageReceived;
                            if (handler != null)
                            {
                                handler.Invoke(sender, args);
                            }

                            message.Dispose();
                            args.Dispose();
                        }
                );
            }
            else
            {
                if (sniffData)
                    Main.Log("Message Received: Tag = " + e.Tag + ", SendMode = " + e.SendMode);

                EventHandler<MessageReceivedEventArgs> handler = MessageReceived;
                if (handler != null)
                {
                    handler.Invoke(sender, e);
                }
            }
        }

        private void Client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            //If we're handling multithreading then pass the event to the dispatcher
            if (invokeFromDispatcher && Dispatcher != null)
            {
                if (!e.LocalDisconnect)
                    Main.mod.Logger.Log("[CLIENT] Disconnected from server, error: " + e.Error);

                Dispatcher.InvokeAsync(
                    () =>
                    {
                        EventHandler<DisconnectedEventArgs> handler = Disconnected;
                        if (handler != null)
                        {
                            handler.Invoke(sender, e);
                        }
                    }
                );
            }
            else
            {
                if (!e.LocalDisconnect)
                    Main.mod.Logger.Log("[CLIENT] Disconnected from server, error: " + e.Error);

                EventHandler<DisconnectedEventArgs> handler = Disconnected;
                if (handler != null)
                {
                    handler.Invoke(sender, e);
                }
            }
        }

        /// <summary>
        ///     Disconnects this client from the server.
        /// </summary>
        /// <returns>Whether the disconnect was successful.</returns>
        public bool Disconnect()
        {
            return Client.Disconnect();
        }

        /// <summary>
        ///     Closes this client.
        /// </summary>
        public void Close()
        {
            if (Client != null)
            {
                Client.MessageReceived -= Client_MessageReceived;
                Client.Disconnected -= Client_Disconnected;

                Client.Dispose();
                Client = null;
            }
        }
    }
}
