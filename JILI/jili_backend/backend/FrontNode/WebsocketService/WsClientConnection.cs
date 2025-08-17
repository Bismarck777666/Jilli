using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Threading;
using System.IO;
using System.Net.WebSockets;
using Akka.Event;
using Akka.IO;

namespace FrontNode.WebsocketService
{
    public class WsClientConnection : ReceiveActor
    {
        private WebSocket                   _wsClient;
        private CancellationTokenSource     _readCancelToken    = new CancellationTokenSource();
        private IActorRef                   _connectionHandler;
        private readonly ILoggingAdapter    _log                = Logging.GetLogger(Context);
        
        private byte[]                      _recvBuffer         = new byte[8 * 12040];

        public class SendStringProtocol
        {
            public string Message { get; }

            public SendStringProtocol(string message)
            {
                Message = message;
            }
        }

        public WsClientConnection(WebSocket wsClient)
        {
            _wsClient = wsClient;

            Receive<RegisterHandler>(registerHandler =>
            {
                _connectionHandler = registerHandler.ConnectionHandler;

                //자료읽기를 시작한다.
                Self.Tell("read");
            });
            Receive<string>                     (processCommand);
            Receive<ReadStreamMessage>          (onReceiveReadStream);
            ReceiveAsync<StringProtocalWrite>   (onStringWriteData);

            Receive<SendStringProtocol>(async msg =>
            {
                if (_wsClient.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(msg.Message);
                    var segment = new ArraySegment<byte>(buffer);

                    try
                    {
                        await _wsClient.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"SendStringProtocol error: {ex.Message}");
                        Self.Tell("disconnected");
                    }
                }
            });
        }
        
        protected override void PreStart()
        {
            base.PreStart();
        }
        
        private void processCommand(string command)
        {
            if(command == "read")
            {
                startReceive();
            }
            else if(command == "disconnected")
            {
                _readCancelToken.Cancel();
                _connectionHandler.Tell(new Disconnected());
                _wsClient.Dispose();
                Context.Stop(Self);
            }
            else if(command == "close")
            {
                _wsClient.Dispose();
            }
        }

        private void startReceive()
        {
            if (_wsClient.State != WebSocketState.Open)
            {
                Self.Tell("disconnected");
                return;
            }
            
            try
            {
                _wsClient.ReceiveAsync(new ArraySegment<byte>(_recvBuffer), _readCancelToken.Token).ContinueWith((readTask =>
                {
                    if(readTask.Status == TaskStatus.Canceled)
                    {
                        _wsClient.Dispose();
                        _connectionHandler.Tell("disconnected");
                        return new ReadStreamMessage(null);
                    }

                    return new ReadStreamMessage(readTask.Result);
                }), TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                //웹소켓에서 자료를 읽는 과정에 례외발생
                _log.Error("Error Reading from web socket " + ex.Message);
                try
                {
                    _wsClient.Dispose();
                    _connectionHandler.Tell("disconnected");
                }
                catch
                {
                }
            }
        }
        
        private void onReceiveReadStream(ReadStreamMessage msgReadStream)
        {
            if (msgReadStream.ReadStream == null)
            {
                Self.Tell("disconnected");
                return;
            }

            try
            {
                if (msgReadStream.ReadStream.Count == 0)
                {
                    Self.Tell("read");
                }
                else if (msgReadStream.ReadStream.MessageType == WebSocketMessageType.Text)
                {
                    var receivedText = Encoding.UTF8.GetString(_recvBuffer, 0, msgReadStream.ReadStream.Count);

                    // 받은 메시지 로그나 핸들링 처리 가능
                    _log.Info($"Received message: {receivedText}");

                    // 클라이언트에 응답 메시지 보내기
                    Self.Tell(new SendStringProtocol("{\"error\":0}"));

                    // 이후 계속 읽기 시작
                    Self.Tell("read");
                }
                else if (msgReadStream.ReadStream.MessageType == WebSocketMessageType.Binary)
                {
                    // 실제 수신된 바이트 수만큼 잘라서 Base64 인코딩
                    var binaryData = new byte[msgReadStream.ReadStream.Count];
                    Array.Copy(_recvBuffer, binaryData, msgReadStream.ReadStream.Count);

                    var base64String = Convert.ToBase64String(binaryData);

                    // 로그 출력
                    _log.Info($"Received Binary message (Base64): {base64String}");

                    // 클라이언트에 JSON 응답
                    Self.Tell(new SendStringProtocol("{\"error\":0}"));

                }
                else
                {
                    Self.Tell("disconnected");
                    return;
                }
            }
            catch
            {
                Self.Tell("read");
            }
        }

        private void onReceiveBinaryData(byte[] buffer, int count)
        {
            // count만큼 buffer에서 실제 데이터를 복사하거나 처리
            var binaryData = new byte[count];
            Array.Copy(buffer, binaryData, count);

            // 예: _connectionHandler에게 바이너리 데이터 전달
            _connectionHandler.Tell(new BinaryProtocolReceived(binaryData));

            // 다시 읽기 시작
            Self.Tell("read");
        }

        public class BinaryProtocolReceived
        {
            public byte[] Data { get; }
            public BinaryProtocolReceived(byte[] data)
            {
                Data = data;
            }
        }

        private async Task onStringWriteData(StringProtocalWrite writeData)
        {
            if (_wsClient.State != WebSocketState.Open)
            {
                Self.Tell("disconnected");
                return;
            }

            try
            {
                await _wsClient.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(writeData.Data)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch(Exception ex)
            {
                //웹소켓에서 자료를 읽는 과정에 례외발생
                _log.Error("AmaticWsClientConnection::onWriteData " + ex.ToString());

                //웹소켓을 닫기한다.
                try
                {
                    _wsClient.Dispose();
                    _connectionHandler.Tell("disconnected");
                }
                catch
                {

                }
            }
        }
        
        private void onReceiveStringData(string receivedData)
        {
            _connectionHandler.Tell(new StringProtocalReceived(receivedData));

            Self.Tell("read");
        }

        public class ReadStreamMessage
        {
            public WebSocketReceiveResult ReadStream { get; private set; }
            public ReadStreamMessage(WebSocketReceiveResult readStream)
            {
                this.ReadStream = readStream;
            }
        }
        
        public class RegisterHandler
        {
            public IActorRef ConnectionHandler { get; private set; }
            public RegisterHandler(IActorRef connectionHandler)
            {
                this.ConnectionHandler = connectionHandler;
            }
        }
        
        public class Disconnected
        {

        }
        
        public class StringProtocalWrite
        {
            public string Data  { get; set; }
            public StringProtocalWrite(string data)
            {
                this.Data = data;
            }
        }
        
        public class StringProtocalReceived
        {
            public string ReceivedData      { get; set; }
            public StringProtocalReceived(string receivedData)
            {
                this.ReceivedData = receivedData;
            }
        }
    }
}
