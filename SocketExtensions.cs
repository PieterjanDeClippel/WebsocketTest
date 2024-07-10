using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace WebsocketTest;

public static class SocketExtensions
{
    public static async Task<string> ReadMessage(this WebSocket ws)
    {
        var buffer = new byte[512];
        byte[] fullMessage = [];
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
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

    public static async Task<T> ReadObject<T>(this WebSocket ws)
    {
        var message = await ws.ReadMessage();
        var obj = JsonConvert.DeserializeObject<T>(message);
        return obj;
    }

    public static async Task WriteObject<T>(this WebSocket ws, T obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        await ws.WriteMessage(json);
    }
}