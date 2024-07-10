using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace WebsocketTest;

public static class SocketExtensions
{
    public static async Task<string> ReadMessage(this WebSocket ws, CancellationToken cancellationToken)
    {
        var buffer = new byte[512];
        byte[] fullMessage = [];
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            fullMessage = fullMessage.Concat(buffer).ToArray();
        }
        while (!result.EndOfMessage);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            throw new WebSocketException("The websocket was closed");
        }

        var message = Encoding.UTF8.GetString(fullMessage);
        return message;
    }

    public static async Task WriteMessage(this WebSocket ws, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public static async Task<T> ReadObject<T>(this WebSocket ws, CancellationToken cancellationToken)
    {
        var message = await ws.ReadMessage(cancellationToken);
        var obj = JsonConvert.DeserializeObject<T>(message);
        return obj;
    }

    public static async Task WriteObject<T>(this WebSocket ws, T obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        await ws.WriteMessage(json);
    }
}