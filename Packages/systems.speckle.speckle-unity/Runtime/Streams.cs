using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorUnity
{
    public static class Streams
    {
        private const string ServerURL = "https://app.speckle.systems";
        private const string authToken = "264a56cc2cbfba65a565a63b0a6a4f6f44f68c999c";
        public static async Task<List<Stream>> List(int limit = 10)
        {
            Account account = new Account()
            {
                token = authToken,
                serverInfo = new ServerInfo(){ url = ServerURL },
            };
            /*var account = AccountManager.GetDefaultAccount();*/
            if (account == null)
                return new List<Stream>();
            var client = new Client(account);

            var res = await client.StreamsGet(limit);

            return res;
        }

        public static async Task<Stream> Get(string streamId, int limit = 10)
        {
            Account account = new Account()
            {
                token = authToken,
                serverInfo = new ServerInfo(){ url = ServerURL },
            };
            /*var account = AccountManager.GetDefaultAccount();*/
            if (account == null)
                return null;
            var client = new Client(account);

            var res = await client.StreamGet(streamId, limit);

            if (res.branches.items != null)
            {
                res.branches.items.Reverse();
            }

            return res;
        }
    }
}
