using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Speckle.ConnectorUnity;
using Speckle.Core.Api;
using UnityEngine;
using UnityEngine.UI;
using Text = UnityEngine.UI.Text;

namespace SpeckleToken
{
    public class ASInteractionLogic : MonoBehaviour
    {
        private Receiver _receiver;

        public void InitReceiver(Stream stream, bool autoReceive)
        {
            gameObject.name = $"receiver-{stream.id}-{Guid.NewGuid().ToString()}";
            InitRemove();

            _receiver = gameObject.AddComponent<Receiver>();
            _receiver.Stream = stream;
            
            _receiver.BranchName = "main";
            
            var btn = gameObject.transform.Find("Btn").GetComponentInChildren<Button>();
            var streamText = gameObject.transform.Find("StreamText").GetComponentInChildren<Text>();
            var statusText = gameObject.transform.Find("StatusText").GetComponentInChildren<Text>();
            
            var receiveProgress = btn.GetComponentInChildren<Slider>();
            receiveProgress.gameObject.SetActive(false); //hide
            
            _receiver.Init(
                stream.id,
                autoReceive,
                /*account:SpeckleToken.instance.Account,*/
                onDataReceivedAction: (go) =>
                {
                    statusText.text = $"Received {go.name}";
                    MakeButtonsInteractable(true);
                    receiveProgress.value = 0;
                    receiveProgress.gameObject.SetActive(false);

                    AddComponents(go);
                },
                onTotalChildrenCountKnown: (count) =>
                {
                    _receiver.TotalChildrenCount = count;
                },
                onProgressAction: (dict) =>
                {
                    //Run on a dispatcher as GOs can only be retrieved on the main thread
                    Dispatcher
                        .Instance()
                        .Enqueue(() =>
                        {
                            var val = dict.Values.Average() / _receiver.TotalChildrenCount;
                            receiveProgress.gameObject.SetActive(true);
                            receiveProgress.value = (float)val;
                        });
                }
            );
            
            streamText.text = $"Stream: {stream.name}\nId: {stream.id} - Auto: {autoReceive}";
            btn.onClick.AddListener(() =>
            {
                statusText.text = "Receiving...";
                MakeButtonsInteractable(false);
                _receiver.Receive();
            });
            
        }
        
        private void AddComponents(GameObject go)
        {
            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);

                if (child.childCount > 0)
                {
                    AddComponents(child.gameObject);
                }

                child.gameObject.AddComponent<Selectable>();

                //Add extra Components
                //var rigidbody = child.gameObject.AddComponent<Rigidbody>();
                //rigidbody.mass = 10;
            }
        }
        
        private void MakeButtonsInteractable(bool interactable)
        {
            var selectables =
                gameObject.transform.GetComponentsInChildren<UnityEngine.UI.Selectable>();
            foreach (var selectable in selectables)
            {
                selectable.interactable = interactable;
            }
        }
        
        private void InitRemove()
        {
            var close = gameObject.transform.Find("Close").GetComponentInChildren<Button>();

            close.onClick.AddListener(() =>
            {
                //remove received geometry
                if (_receiver != null)
                {
                    Destroy(_receiver.ReceivedData);
                }

                //update ui
                GameObject
                    .Find("ASSpeckleExample")
                    .GetComponent<ASSpeckleExample>()
                    .RemoveStreamPrefab(gameObject);

                //kill it
                Destroy(gameObject);
            });
        }
    }
}
