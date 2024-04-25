#nullable enable

namespace Microsoft.Extensions.Configuration.AzureUserData;

using System;
using Microsoft.Extensions.Configuration;

public class AzureUserDataConfigurationProvider : ConfigurationProvider
{
    private readonly AzureUserDataConfigurationSource source;

    public AzureUserDataConfigurationProvider(AzureUserDataConfigurationSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.source.Refreshed += this.OnSourceReloaded;
    }

    public override void Load()
        => this.Load(false);

    private void OnSourceReloaded(object? sender, EventArgs e)
        => this.Load(true);

    private void Load(bool reload)
    {
        try
        {
            if (this.source.Stream is null)
            {
                throw new InvalidOperationException("AzureUserDataConfigurationSource Stream cannot be null");
            }

            // Parse the stream into Data
            var parser = new JsonStreamConfigurationProviderExposer();
            base.Data = parser.LoadData(this.source.Stream);
        }
        catch
        {
            if (this.source.Optional || reload) // Always optional on reload
            {
                return;
            }

            throw;
        }
    }
}
