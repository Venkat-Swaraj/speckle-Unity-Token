using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using UnityEngine;

namespace SpeckleToken
{
    public class SpeckleToken : MonoBehaviour
    {
        private const string ServerURL = "https://app.speckle.systems";
        [SerializeField]
        private string authToken = "264a56cc2cbfba65a565a63b0a6a4f6f44f68c999c";

        public static SpeckleToken instance;
        public Account Account;

        private void Awake()
        {
            if (instance == null)
                instance = this;

            Account = new Account()
            {
                token = authToken,
                serverInfo = new ServerInfo(){ url = ServerURL },
            };
        }
    
    }
}
