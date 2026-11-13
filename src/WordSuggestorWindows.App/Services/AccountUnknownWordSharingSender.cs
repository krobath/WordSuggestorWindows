using System.Text.Json;
namespace WordSuggestorWindows.App.Services;
public sealed class AccountUnknownWordSharingSender(AccountSessionCoordinator account):IUnknownWordSharingSender
{
    public string Origin=>account.Origin;
    public long UserId=>account.Profile?.UserId??0;
    public bool Ready=>UnknownWordSharingDeployment.Qualified&&account.IsSignedIn&&!account.Busy&&account.Config.Supports(UnknownWordSharingDeployment.Capability);
    public Task SendAsync(JsonElement body,string key,CancellationToken token)=>account.SubmitUnknownWordHandoffAsync(body,key,token);
}
