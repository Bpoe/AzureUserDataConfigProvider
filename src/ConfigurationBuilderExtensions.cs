#nullable enable

namespace Microsoft.Extensions.Configuration.AzureUserData;

using System;
using Microsoft.Extensions.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddAzureUserData(this IConfigurationBuilder configurationBuilder)
        => configurationBuilder.Add(new AzureUserDataConfigurationSource());

    public static IConfigurationBuilder AddAzureUserData(this IConfigurationBuilder configurationBuilder, Uri userDataUri)
        => configurationBuilder.Add(new AzureUserDataConfigurationSource
        {
            UserDataUri = userDataUri.ToString(),
        });
}