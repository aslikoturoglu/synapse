using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Core;

namespace Server.Services.AiFoundry;

// Bridges the classic Azure.Core.TokenCredential (ClientSecretCredential/DefaultAzureCredential)
// to the newer System.ClientModel.AuthenticationTokenProvider that AIProjectClient expects —
// no built-in adapter exists yet in these (still-preview) SDK versions.
public class TokenCredentialAuthenticationProvider(TokenCredential credential, string[] scopes) : AuthenticationTokenProvider
{
    public override GetTokenOptions CreateTokenOptions(IReadOnlyDictionary<string, object> properties) =>
        new(properties);

    public override AuthenticationToken GetToken(GetTokenOptions options, CancellationToken cancellationToken)
    {
        var token = credential.GetToken(new TokenRequestContext(scopes), cancellationToken);
        return new AuthenticationToken(token.Token, "Bearer", token.ExpiresOn, null);
    }

    public override async ValueTask<AuthenticationToken> GetTokenAsync(GetTokenOptions options, CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopes), cancellationToken);
        return new AuthenticationToken(token.Token, "Bearer", token.ExpiresOn, null);
    }
}
