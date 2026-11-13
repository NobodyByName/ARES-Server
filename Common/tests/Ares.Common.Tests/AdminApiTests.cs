using System.Net;
using System.Text;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Ares.Common.Tests;

public sealed class AdminApiTests : IAsyncLifetime
{
    private const string Token = "operator-token-operator-token-operator-token";
    private readonly FakeAdmin _admin = new();
    private WebApplication? _app;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        _app = builder.Build();
        _app.MapAdminApi(_admin, Token);
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app!.DisposeAsync();
    }

    [Fact]
    public async Task A_command_with_the_operator_token_runs_and_returns_its_reply()
    {
        using SessionAllocatorClient client = new(new Uri("http://localhost"), Token, _app!.GetTestServer().CreateHandler());
        (int status, AdminReply reply) = await client.AdminAsync(new AdminCommand("teleport", ["42", "1", "2", "3"]), TestContext.Current.CancellationToken);

        Assert.Equal(200, status);
        Assert.Equal("teleport 42 1 2 3", Assert.Single(reply.Lines));
        Assert.Null(reply.Error);
        Assert.Single(_admin.Received);
    }

    [Fact]
    public async Task Calls_without_the_operator_token_are_rejected_before_any_command_runs()
    {
        using SessionAllocatorClient wrong = new(new Uri("http://localhost"), Token.ToUpperInvariant(), _app!.GetTestServer().CreateHandler());
        (int status, AdminReply reply) = await wrong.AdminAsync(new AdminCommand("players", []), TestContext.Current.CancellationToken);
        using HttpClient anonymous = _app!.GetTestClient();
        using StringContent body = new("""{"name":"players","arguments":[]}""", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await anonymous.PostAsync(AdminApi.CommandPath, body, TestContext.Current.CancellationToken);

        Assert.Equal((401, AdminApi.AuthenticationRequired), (status, reply.Error));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_admin.Received);
    }

    [Theory]
    [InlineData("""{"name":"players"}""")]
    [InlineData("""{"arguments":[]}""")]
    [InlineData("""{"name":"players","arguments":["1","2","3","4","5","6","7","8","9"]}""")]
    [InlineData("not json")]
    public async Task Malformed_requests_are_rejected(string json)
    {
        using HttpClient http = _app!.GetTestClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + Token);
        using StringContent body = new(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await http.PostAsync(AdminApi.CommandPath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_admin.Received);
    }

    private sealed class FakeAdmin : IAdminService
    {
        public List<AdminCommand> Received { get; } = [];

        public Task<AdminResult> RunAsync(AdminCommand command, CancellationToken cancellationToken)
        {
            Received.Add(command);
            return Task.FromResult(new AdminResult(StatusCodes.Status200OK, new AdminReply([string.Join(' ', [command.Name, .. command.Arguments])])));
        }
    }
}
