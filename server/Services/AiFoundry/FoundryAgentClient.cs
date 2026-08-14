using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using OpenAI.Files;
using OpenAI.Responses;

namespace Server.Services.AiFoundry;

// Thin wrapper around Azure.AI.Projects — every other AiFoundry service is built on this one
// class, nothing else talks to the SDK directly. This project's Foundry setup uses the newer,
// session-less Responses API (confirmed against the project's own portal-generated sample
// code): there's no explicit Thread/Run/Message model here — conversation continuity is just
// OpenAI's own previousResponseId chaining, and agents are addressed by name, not a separate
// opaque id. Auth is Entra ID only — this SDK has no raw-API-key option. The org's tenant
// (Sabancı University) doesn't allow self-service App Registration creation, so there's no
// Service Principal path for now; falls back to AzureCliCredential (`az login`, already
// authenticated as the same account that has access to the Foundry project itself). Not
// DefaultAzureCredential — its default chain tries ManagedIdentityCredential first, which
// times out slowly (IMDS probe) in this non-Azure dev environment before ever reaching the
// CLI credential.
public class FoundryAgentClient
{
    // The exact resource scope AI Foundry's data-plane APIs expect for Entra ID token auth.
    private static readonly string[] Scopes = ["https://ai.azure.com/.default"];

    private readonly AIProjectClient _client;

    public FoundryAgentClient(IConfiguration configuration)
    {
        var endpoint = configuration["AzureAiFoundry:Endpoint"]
            ?? throw new InvalidOperationException("AzureAiFoundry:Endpoint is not configured.");

        var tenantId = configuration["AzureAiFoundry:TenantId"];
        var clientId = configuration["AzureAiFoundry:ClientId"];
        var clientSecret = configuration["AzureAiFoundry:ClientSecret"];

        TokenCredential credential = !string.IsNullOrWhiteSpace(tenantId)
            && !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret)
            ? new ClientSecretCredential(tenantId, clientId, clientSecret)
            : new AzureCliCredential();

        _client = new AIProjectClient(new Uri(endpoint), new TokenCredentialAuthenticationProvider(credential, Scopes));
    }

    public async Task<string> UploadFileAsync(byte[] bytes, string fileName)
    {
        var fileClient = _client.ProjectOpenAIClient.GetOpenAIFileClient();
        using var stream = new MemoryStream(bytes);
        var file = await fileClient.UploadFileAsync(stream, fileName, FileUploadPurpose.Assistants);
        return file.Value.Id;
    }

    // Sends one message to the named agent, optionally continuing a prior exchange
    // (previousResponseId) and optionally referencing already-uploaded files. Returns the
    // answer text and this response's own id — pass that id back in as previousResponseId on
    // the next call to keep the same conversation going.
    public async Task<(string Text, string ResponseId)> AskAsync(
        string agentName, string text, string? previousResponseId = null, IEnumerable<string>? fileIds = null)
    {
        var responsesClient = _client.ProjectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(agentName, null, null);

        var parts = new List<ResponseContentPart>();
        if (fileIds is not null)
            parts.AddRange(fileIds.Select(ResponseContentPart.CreateInputFilePart));
        parts.Add(ResponseContentPart.CreateInputTextPart(text));

        var options = new CreateResponseOptions
        {
            InputItems = { ResponseItem.CreateUserMessageItem(parts) },
            PreviousResponseId = previousResponseId,
        };

        var response = await responsesClient.CreateResponseAsync(options);
        return (response.Value.GetOutputText(), response.Value.Id);
    }
}
