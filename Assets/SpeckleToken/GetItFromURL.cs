using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorUnity.Components;
using Speckle.ConnectorUnity.Utils;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using UnityEngine;

namespace SpeckleToken
{
    [RequireComponent(typeof(RecursiveConverter)), ExecuteAlways]
    public class GetItFromURL : MonoBehaviour
    {
        public string url = "https://app.speckle.systems/projects/58f24a992a/models/6d05acd6dc";
        
        private RecursiveConverter _converter;
#nullable enable
        private CancellationTokenSource? _tokenSource;

        void Awake()
        {
            _converter = GetComponent<RecursiveConverter>();
        }
        [ContextMenu(nameof(Receive))]
        public void Receive()
        {
            StartCoroutine(Receive_Routine());
        }
        
        public IEnumerator Receive_Routine()
        {
            if (IsBusy())
                throw new InvalidOperationException("A receive operation has already started");
            _tokenSource = new CancellationTokenSource();
            try
            {
                StreamWrapper sw = new(url);

                if (!sw.IsValid)
                    throw new InvalidOperationException(
                        "Speckle url input is not a valid speckle stream/branch/commit"
                    );

                var accountTask = new Utils.WaitForTask<Account>(
                    async () => await GetAccount(sw),
                    _tokenSource.Token
                );
                yield return accountTask;

                _tokenSource.Token.ThrowIfCancellationRequested();
                using Client c = new(accountTask.Result);

                var objectIdTask = new Utils.WaitForTask<(string, Commit?)>(
                    async () => await GetObjectID(sw, c),
                    _tokenSource.Token
                );

                yield return objectIdTask;
                (string objectId, Commit? commit) = objectIdTask.Result;
                c.SubscribeCommitCreated(sw.StreamId);
                c.OnCommitCreated += Client_OnCommitCreated;
                Debug.Log($"Receiving from {sw.ServerUrl}...");

                var receiveTask = new Utils.WaitForTask<Base>(
                    async () =>
                        await SpeckleReceiver.ReceiveAsync(
                            c,
                            sw.StreamId,
                            objectId,
                            commit,
                            cancellationToken: _tokenSource.Token
                        ),
                    _tokenSource.Token
                );
                yield return receiveTask;

                Debug.Log("Converting to native...");
                _converter.RecursivelyConvertToNative_Sync(receiveTask.Result, transform);
            }
            finally
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }
        protected virtual void Client_OnCommitCreated(object sender, CommitInfo e)
        {
            if (e.branchName == "main")
            {
                Debug.Log("New commit created");
            }
        }
        private async Task<(string objectId, Commit? commit)> GetObjectID(
            StreamWrapper sw,
            Client client
        )
        {
            string objectId;
            Commit? commit = null;
            //OBJECT URL
            if (!string.IsNullOrEmpty(sw.ObjectId))
            {
                objectId = sw.ObjectId;
            }
            //COMMIT URL
            else if (!string.IsNullOrEmpty(sw.CommitId))
            {
                commit = await client.CommitGet(sw.StreamId, sw.CommitId).ConfigureAwait(false);
                objectId = commit.referencedObject;
            }
            //BRANCH URL OR STREAM URL
            else
            {
                var branchName = string.IsNullOrEmpty(sw.BranchName) ? "main" : sw.BranchName;

                var branch = await client
                    .BranchGet(sw.StreamId, branchName, 1)
                    .ConfigureAwait(false);
                if (!branch.commits.items.Any())
                    throw new SpeckleException("The selected branch has no commits.");

                commit = branch.commits.items[0];
                objectId = branch.commits.items[0].referencedObject;
            }

            return (objectId, commit);
        }
        
        [ContextMenu(nameof(Cancel))]
        public void Cancel()
        {
            if (IsNotBusy())
                throw new InvalidOperationException(
                    "There are no pending receive operations to cancel"
                );
            _tokenSource!.Cancel();
        }

        [ContextMenu(nameof(Cancel), true)]
        public bool IsBusy()
        {
            return _tokenSource is not null;
        }

        [ContextMenu(nameof(Receive), true)]
        internal bool IsNotBusy() => !IsBusy();

        private void OnDisable()
        {
            _tokenSource?.Cancel();
        }

        private Task<Account> GetAccount(StreamWrapper sw)
        {
            return Task.FromResult(SpeckleToken.instance.Account);
        }
        
        
    }
}
