using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Speckle.ConnectorUnity.Components;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using UnityEngine;
using Speckle.Core.Api;
using Speckle.ConnectorUnity;
using Speckle.Core.Models;

namespace SpeckleToken
{
    [RequireComponent(typeof(RecursiveConverter))]
    public class AutoReceiver : MonoBehaviour
    {
        // From Reciever.cs
        public string StreamId;
        public string BranchName = "main";
        public Stream Stream;
        public int TotalChildrenCount = 0;
        public GameObject ReceivedData;

        private bool AutoReceive;
        private bool DeleteOld;
        private Action<ConcurrentDictionary<string, int>> OnProgressAction;
        private Action<string, Exception> OnErrorAction;
        private Action<int> OnTotalChildrenCountKnown;
        private Action<GameObject> OnDataReceivedAction;
        
        private Client Client { get; set; }
        // receiver cs
        
        /// <summary>
        /// Initializes the Receiver manually
        /// </summary>
        /// <param name="streamId">Id of the stream to receive</param>
        /// <param name="autoReceive">If true, it will automatically receive updates sent to this stream</param>
        /// <param name="deleteOld">If true, it will delete previously received objects when new one are received</param>
        /// <param name="account">Account to use, if null the default account will be used</param>
        /// <param name="onDataReceivedAction">Action to run after new data has been received and converted</param>
        /// <param name="onProgressAction">Action to run when there is download/conversion progress</param>
        /// <param name="onErrorAction">Action to run on error</param>
        /// <param name="onTotalChildrenCountKnown">Action to run when the TotalChildrenCount is known</param>
        public void Init(
            string streamId,
            bool autoReceive = false,
            bool deleteOld = true,
            Account account = null,
            Action<GameObject> onDataReceivedAction = null,
            Action<ConcurrentDictionary<string, int>> onProgressAction = null,
            Action<string, Exception> onErrorAction = null,
            Action<int> onTotalChildrenCountKnown = null
        )
        {
            StreamId = streamId;
            AutoReceive = autoReceive;
            DeleteOld = deleteOld;
            OnDataReceivedAction = onDataReceivedAction;
            OnErrorAction = onErrorAction;
            OnProgressAction = onProgressAction;
            OnTotalChildrenCountKnown = onTotalChildrenCountKnown;
            if (account == null)
            {
                Debug.Log("account is null in Reciever");
            }
            Client = new Client(account ?? AccountManager.GetDefaultAccount());

            if (AutoReceive)
            {
                Client.SubscribeCommitCreated(StreamId);
                Client.OnCommitCreated += Client_OnCommitCreated;
            }
        }

        protected virtual void Client_OnCommitCreated(object sender, CommitInfo e)
        {
            if (e.branchName == BranchName)
            {
                Debug.Log("New commit created");
                /*GetAndConvertObject(e.objectId, e.id, e.sourceApplication, e.authorId);*/
            }
        }

        /// <summary>
        /// Receives the requested <see cref="objectId"/> using async Task
        /// </summary>
        /// <param name="client"></param>
        /// <param name="streamId"></param>
        /// <param name="objectId"></param>
        /// <param name="commit"></param>
        /// <param name="onProgressAction"></param>
        /// <param name="onTotalChildrenCountKnown"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Exception">Throws various types of exceptions to indicate faliure</exception>
        /// <returns>The requested Speckle object</returns>
        public static async Task<Base> ReceiveAsync(
            Client client,
            string streamId,
            string objectId,
            Commit? commit,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<int>? onTotalChildrenCountKnown = null,
            CancellationToken cancellationToken = default
        )
        {
            using var transport = new ServerTransport(client.Account, streamId);

            transport.CancellationToken = cancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            Base requestedObject = await Operations
                .Receive(
                    objectId,
                    transport,
                    null,
                    onProgressAction,
                    onTotalChildrenCountKnown,
                    cancellationToken
                )
                .ConfigureAwait(false);

            Analytics.TrackEvent(
                client.Account,
                Analytics.Events.Receive,
                new Dictionary<string, object>()
                {
                    { "mode", nameof(SpeckleReceiver) },
                    {
                        "sourceHostApp",
                        HostApplications.GetHostAppFromString(commit?.sourceApplication).Slug
                    },
                    { "sourceHostAppVersion", commit?.sourceApplication ?? "" },
                    { "hostPlatform", Application.platform.ToString() },
                    {
                        "isMultiplayer",
                        commit?.authorId != null && commit?.authorId != client.Account?.userInfo?.id
                    },
                }
            );

            cancellationToken.ThrowIfCancellationRequested();

            //Read receipt
            try
            {
                await client
                    .CommitReceived(
                        new CommitReceivedInput
                        {
                            streamId = streamId,
                            commitId = commit?.id,
                            message = $"received commit from {Application.unityVersion}",
                            sourceApplication = HostApplications.Unity.GetVersion(
                                CoreUtils.GetHostAppVersion()
                            )
                        },
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Do nothing!
                Debug.LogWarning($"Failed to send read receipt\n{ex}");
            }

            return requestedObject;
        }
        
    }
}
