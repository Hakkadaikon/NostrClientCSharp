using Secp256k1Net;
using System.Net.WebSockets;
using System.Text;

namespace NostrClientCSharpTest
{
    [TestClass]
    public class UnitTest1
    {
        private static string CreateNostrPost(byte[] publicKey, byte[] privateKey, string message)
        {
            string pubKeyHex = BitConverter.ToString(publicKey).Replace("-", "").ToLower();
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string id = Guid.NewGuid().ToString();

            //string postTemplate = "{\"id\":\"{0}\",\"pubkey\":\"{1}\",\"created_at\":{2},\"kind\":1,\"tags\":[],\"content\":\"{3}\",\"sig\":\"{4}\"}";
            string unsignedPost = $"{{\"id\":\"{id}\",\"pubkey\":\"{pubKeyHex}\",\"created_at\":{timestamp},\"kind\":1,\"tags\":[],\"content\":\"{message}\",\"sig\":\"\"}}";

            //string unsignedPost = string.Format(postTemplate, id, pubKeyHex, timestamp, message, "");
            byte[] unsignedPostBytes = Encoding.UTF8.GetBytes(unsignedPost);
            var buffer = new byte[1024];

            byte[] signature = new byte[64];
            using (var secp256k1 = new Secp256k1())
            {
                secp256k1.Sign(signature, unsignedPostBytes, privateKey);
            }

            string signatureHex = BitConverter.ToString(signature).Replace("-", "").ToLower();
            //string signedPost = string.Format(postTemplate, id, pubKeyHex, timestamp, message, signatureHex);
            string signedPost = $"{{\"id\":\"{id}\",\"pubkey\":\"{pubKeyHex}\",\"created_at\":{timestamp},\"kind\":1,\"tags\":[],\"content\":\"{message}\",\"sig\":\"{signatureHex}\"}}";
            return signedPost;
        }

        private async Task Post()
        {
            Uri uri = new("wss://yabu.me/");

            byte[] privateKey = new byte[32];
            new Random().NextBytes(privateKey);
            byte[] publicKey = new byte[64];

            using (var secp256k1 = new Secp256k1())
            {
                secp256k1.SecretKeyVerify(privateKey);
                secp256k1.PublicKeyCreate(publicKey, privateKey);
            }

            string message = "Hello, Nostr! blazor app test - csharp mob programming";
            string postContent = CreateNostrPost(publicKey, privateKey, message);

            using ClientWebSocket ws = new();
            await ws.ConnectAsync(uri, default);

            var eventJson = "[\"EVENT\"," + postContent + "]";
            await ws.SendAsync(Encoding.UTF8.GetBytes(eventJson), WebSocketMessageType.Text, true, default);

            var buffer = new byte[1024];
            ArraySegment<byte> segment = new(buffer);
            var result = await ws.ReceiveAsync(segment, default);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", default);
                return;
            }

        }

        [TestMethod]
        public async Task TestMethod1()
        {
            await Post();
        }
    }
}