using System;
using System.Collections.Generic;
using UnityEngine;
using Speckle.Core.Credentials;
using System.Linq;
using UnityEngine.UI;
using Speckle.ConnectorUnity;
using Streams = Speckle.ConnectorUnity.Streams;

namespace SpeckleToken
{
    public class ASSpeckleExample : MonoBehaviour
    {
        public GameObject StreamPanel;
        public Canvas StreamsCanvas;
        private List<GameObject> StreamPanels = new List<GameObject>();
        
        void Start()
        {
            var defaultAccount = SpeckleToken.instance.Account;
            if (defaultAccount == null)
            {
                Debug.Log("Please set a default account in SpeckleManager");
                return;
            }
        }

        public void CallAddRecviever()
        {
            AddReceiver();
        }
        
        private async void AddReceiver()
        {
            var stream = await Streams.Get("58f24a992a", 30);

            var streamPrefab = Instantiate(StreamPanel, new Vector3(0, 0, 0), Quaternion.identity);

            //set position
            streamPrefab.transform.SetParent(StreamsCanvas.transform);
            var rt = streamPrefab.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector3(-10, -110 - StreamPanels.Count * 110, 0);

            streamPrefab.AddComponent<InteractionLogic>().InitReceiver(stream, true);

            StreamPanels.Add(streamPrefab);
        }
        // Optional
        public void RemoveStreamPrefab(GameObject streamPrefab)
        {
            StreamPanels.RemoveAt(StreamPanels.FindIndex(x => x.name == streamPrefab.name));
            ReorderStreamPrefabs();
        }

        private void ReorderStreamPrefabs()
        {
            for (var i = 0; i < StreamPanels.Count; i++)
            {
                var rt = StreamPanels[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector3(-10, -110 - i * 110, 0);
            }
        }
    }
}
