﻿using Mixer.Base.Model.Client;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Util
{
    public abstract class WebSocketServerBase
    {
        private const int bufferSize = 1000000;

        private UTF8Encoding encoder = new UTF8Encoding();

        private string address;

        private HttpListener httpListener;
        private WebSocket webSocket;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public WebSocketServerBase(string address) { this.address = address; }

        public Task<bool> Initialize()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async() => { await this.ListenForConnection(); }, this.tokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            return Task.FromResult(true);
        }

        private async Task ListenForConnection()
        {
            while (true)
            {
                try
                {
                    this.httpListener = new HttpListener();
                    this.httpListener.Prefixes.Add(this.address);
                    this.httpListener.Start();

                    var hc = await this.httpListener.GetContextAsync();
                    if (!hc.Request.IsWebSocketRequest)
                    {
                        hc.Response.StatusCode = 400;
                        hc.Response.Close();
                    }
                    else
                    {
                        var wsc = await hc.AcceptWebSocketAsync(null);
                        this.webSocket = wsc.WebSocket;

                        await this.ReceiveInternal();
                    }

                    await this.ShutdownListeners();
                }
                catch (Exception ex)
                {
                    await this.ShutdownListeners();

                    Logger.Log(ex);
                }

                this.tokenSource.Token.ThrowIfCancellationRequested();
            }
        }

        public async Task Send(WebSocketPacket packet)
        {
            string packetJson = JsonConvert.SerializeObject(packet);
            byte[] buffer = this.encoder.GetBytes(packetJson);
            await this.webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task Disconnect()
        {
            this.tokenSource.Cancel();
            await this.ShutdownListeners();
        }

        protected abstract Task PacketReceived(WebSocketPacket packet);

        private async Task ReceiveInternal()
        {
            byte[] buffer = new byte[WebSocketServerBase.bufferSize];
            while (this.webSocket != null)
            {
                if (this.webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        Array.Clear(buffer, 0, buffer.Length);
                        WebSocketReceiveResult result = await this.webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result != null)
                        {
                            if (result.CloseStatus == null || result.CloseStatus != WebSocketCloseStatus.Empty)
                            {
                                try
                                {
                                    string jsonBuffer = this.encoder.GetString(buffer);
                                    dynamic jsonObject = JsonConvert.DeserializeObject(jsonBuffer);
                                    WebSocketPacket packet = JsonConvert.DeserializeObject<WebSocketPacket>(jsonBuffer);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                    this.PacketReceived(packet);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        private async Task ShutdownListeners()
        {
            try
            {
                if (this.webSocket != null)
                {
                    await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
                }
            }
            catch (Exception ex) { Logger.Log(ex); }

            this.webSocket = null;

            if (this.httpListener != null)
            {
                this.httpListener.Stop();
                this.httpListener.Close();
                this.httpListener = null;
            }


        }
    }
}