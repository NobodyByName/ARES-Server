using System.Text;
using Ares.Structs.Backend.Telemetry;

namespace Ares.Structs.Tests.Backend;

public sealed class TelemetryTests
{
    [Fact]
    public void Empty_ok_response_is_an_empty_message_and_a_status_trailer()
    {
        byte[] response = GrpcWeb.EmptyOkResponse();

        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0x80, 0, 0, 0, 16 }, response[..10]);
        Assert.Equal("grpc-status: 0\r\n", Encoding.ASCII.GetString(response, 10, 16));
    }

    [Fact]
    public void Ui_events_are_decoded_from_a_batch()
    {
        byte[] navigation = [.. Field(2, "UI.Nav.MainMenu"), .. Field(3, "UI.Nav.Boot"), .. Field(4, "MainMenu")];
        byte[] error = Field(3, "ARSQ0003");
        byte[] record = [.. Field(1051, navigation), .. Field(1105, error), .. Field(9999, [1, 2])];
        byte[] message = Field(1, record);
        byte[] body = [0, 0, 0, 0, (byte)message.Length, .. message, 0x80, 0, 0, 0, 0];

        IReadOnlyList<UiTelemetryEvent> events = UiTelemetry.ReadEvents(body);

        Assert.Equal([new UiNavigationEvent("UI.Nav.MainMenu", "UI.Nav.Boot", "MainMenu"), new UiErrorEvent(null, "ARSQ0003", null)], events);
    }

    [Fact]
    public void Truncated_batches_return_what_decoded()
    {
        Assert.Empty(UiTelemetry.ReadEvents(new byte[] { 0, 0, 0, 0, 9, 1 }));
    }

    private static byte[] Field(int number, string text)
    {
        return Field(number, Encoding.UTF8.GetBytes(text));
    }

    private static byte[] Field(int number, byte[] data)
    {
        return [.. Varint((ulong)((number << 3) | 2)), .. Varint((ulong)data.Length), .. data];
    }

    private static byte[] Varint(ulong value)
    {
        List<byte> bytes = [];
        do
        {
            byte b = (byte)(value & 0x7f);
            value >>= 7;
            bytes.Add(value != 0 ? (byte)(b | 0x80) : b);
        }
        while (value != 0);

        return [.. bytes];
    }
}
