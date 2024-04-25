#nullable enable

namespace Microsoft.Extensions.Configuration.AzureUserData;

using System.IO;
using Microsoft.Extensions.Configuration.Json;
using System.Collections.Generic;

public class JsonStreamConfigurationProviderExposer : JsonStreamConfigurationProvider
{
    public JsonStreamConfigurationProviderExposer()
        : base(new JsonStreamConfigurationSource())
    {
    }

    public IDictionary<string, string?> LoadData(Stream stream)
    {
        // We just need this class to get access to the functionality to parse a JSON stream into a Dictionary.
        // Unfortunately, all these classes and methods are private or internal.
        // This little hack lets use re-use them.
        this.Load(stream);
        return this.Data;
    }
}