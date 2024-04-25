#nullable enable

namespace Microsoft.Extensions.Configuration.AzureUserData;

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;

public class AzureUserDataConfigurationSource : StreamConfigurationSource, IDisposable
{
    private readonly HttpClient httpClient;
    private readonly CancellationTokenSource cancellationToken = new();

    private Task? refreshTask;
    private bool disposedValue;

    public AzureUserDataConfigurationSource(HttpClient? client = null)
    {
        this.httpClient = client ?? new HttpClient();
    }

    public bool Optional { get; set; } = true;

    public TimeSpan? ReloadInterval { get; set; } = TimeSpan.FromMinutes(10);

    public string UserDataUri { get; set; } = "http://169.254.169.254/metadata/instance/compute/userData";

    public string ApiVersion { get; set; } = "2021-01-01";

    public event EventHandler? Refreshed;

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        this.ReadAsync(this.cancellationToken.Token).Wait();

        // Schedule a polling task to periodically reload,
        // but only if none exists and a valid delay is specified
        if (this.refreshTask is null && this.ReloadInterval is not null)
        {
            this.refreshTask = this.PeriodicallyRefreshAsync();
        }

        return new AzureUserDataConfigurationProvider(this);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposedValue)
        {
            return;
        }

        if (disposing)
        {
            this.cancellationToken.Cancel();
            this.cancellationToken.Dispose();
            this.refreshTask?.Dispose();
        }

        this.disposedValue = true;
    }

    protected virtual void OnRefreshed(EventArgs e)
    {
        // Make a temporary copy of the event to avoid possibility of
        // a race condition if the last subscriber unsubscribes
        // immediately after the null check and before the event is raised.
        var refreshedEvent = Refreshed;

        // Event will be null if there are no subscribers
        if (refreshedEvent is not null)
        {
            refreshedEvent(this, e);
        }
    }

    private async Task PeriodicallyRefreshAsync()
    {
        if (this.ReloadInterval is null)
        {
            return;
        }

        while (!this.cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(this.ReloadInterval.Value, this.cancellationToken.Token);
            await this.ReadAsync(this.cancellationToken.Token);
        }
    }

    private async Task ReadAsync(CancellationToken cancellationToken = default)
    {
        using var response = await this.GetUserData(cancellationToken);
        this.Stream = await FromBase64(await response.Content.ReadAsStreamAsync());

        this.OnRefreshed(EventArgs.Empty);
    }

    private async Task<HttpResponseMessage> GetUserData(CancellationToken cancellationToken = default)
    {
        var uri = $"{this.UserDataUri}?api-version={this.ApiVersion}&format=text";

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("metadata", "true");

        var response = await this.httpClient.SendAsync(request, cancellationToken);
        return response.EnsureSuccessStatusCode();
    }

    private static async Task<Stream> FromBase64(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var encodedString = await reader.ReadToEndAsync();

        var userDataBytes = Convert.FromBase64String(encodedString);
        return new MemoryStream(userDataBytes);
    }
}
