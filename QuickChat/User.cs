using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickChat
{
    record ChatMessage(int id, string msg, string user);
    public class User : IDisposable
    {
        public static event EventHandler MessageReceived;
        private byte[] buffer = new byte[4096];
        private string username;
        private WebSocket socket;
        private static Queue<ChatMessage> lastMessages = new Queue<ChatMessage>();
        private static int msgcount = 1;
        private static object messagelock = new object();
        private bool disposedValue;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public User(string username, WebSocket socket)
        {
            this.username = username;
            this.socket = socket;
            MessageReceived += SendHandler;
        }
        public bool MsgHandler(WebSocketReceiveResult result)
        {
            try
            {
                if (result.CloseStatus == null)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    lock (messagelock)
                    {
                        lastMessages.Enqueue(new ChatMessage(msgcount++, msg, username));
                        while (lastMessages.Count > 10)
                        {
                            lastMessages.Dequeue();
                        }
                    }
                    MessageReceived?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            catch { }
            return false;
        }
        public Task<WebSocketReceiveResult> Receive() => socket.ReceiveAsync(buffer, cts.Token);
        private async void SendHandler(object sender, EventArgs e) => await Send();
        public async Task Send()
        {
            try
            {
                byte[] toSend = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(lastMessages.ToArray()));
                await socket.SendAsync(toSend, WebSocketMessageType.Text, true, cts.Token);
            }
            catch { }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        cts.Cancel();
                        MessageReceived -= SendHandler;
                    }
                    catch { }
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~User()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
