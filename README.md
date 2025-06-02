To run this function, you will need a ./src/Defra.Trade.Events.IDCOMS.GCEnricher/local.settings.json file. The file will need the following structure:

```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "FUNCTIONS_EXTENSION_VERSION": "~4",
        "FUNCTIONS_INPROC_NET8_ENABLED": "1",
        "ServiceBus:ConnectionString": "<secret>",
        "ConfigurationServer:ConnectionString": "<secret>",
        "ConfigurationServer:TenantId": "<secret>"
    }
}
```

Secrets reference can be found here: https://dev.azure.com/defragovuk/DEFRA-TRADE-APIS/_wiki/wikis/DEFRA-TRADE-APIS.wiki/26086
