using System.Text.Json;

namespace WordSuggestorWindows.App.Models;

public sealed record AccountUser(long Id, string Email, bool EmailVerified);
public sealed record AccountTokens(string AccessToken, string TokenType, int ExpiresIn, string? RefreshToken,
    DateTimeOffset? RefreshIdleExpiresAt, DateTimeOffset? RefreshAbsoluteExpiresAt, string? SessionId,
    string? SessionRevocationSecret, string? AuthContractVersion, AccountUser User)
{ public override string ToString() => "AccountTokens[REDACTED]"; }
public sealed record AccountProfile(long UserId, string Email, bool EmailVerified, string? DisplayName);
public sealed record AccountPersonalization(string SchemaName, long ProfileVersion, string? AgeBand,
    string? ProfessionCode, string? IndustryCode, bool PersonalizationEnabled);
public sealed record AccountEntitlement(string Product, string Tier, string Status, DateTimeOffset? ValidUntil);
public sealed record AccountEntitlements(AccountEntitlement? Entitlement);
public sealed record AccountConfig(Dictionary<string, bool> Flags, bool KillSwitch = false)
{ public bool Supports(string key) => !KillSwitch && Flags?.GetValueOrDefault(key) == true; }
public sealed record VerificationPending(string Status, string MessageCode, long VerificationWindowSeconds, long ResendAfterSeconds);
public sealed record AccountRecovery(int Version, string Origin, string DeviceId, string RefreshToken, string SessionId,
    string RevocationSecret, DateTimeOffset IdleExpiry, DateTimeOffset AbsoluteExpiry, long UserId, string? RefreshRequestId = null)
{ public override string ToString() => "AccountRecovery[REDACTED]"; }
public enum AccountState { SignedOut, Connecting, SignedIn, Locked, OfflineRecoverable, ReauthenticationRequired }
public sealed class AccountFailure(string code, int status = 0) : Exception("Account operation failed.")
{ public string Code { get; } = code; public int Status { get; } = status; public override string ToString() => $"AccountFailure[{Code},{Status}]"; }

public static class AccountCatalogs
{
    // Canonical IDs: SettingsNavigationPolicy.swift ba18b45 and backend profile contract.
    public static readonly string[] Ages = ["", "under_18", "18_24", "25_34", "35_44", "45_54", "55_64", "65_plus", "prefer_not_to_say"];
    public static readonly string[] Professions = ["", "student", "educator", "healthcare_professional", "legal_professional", "finance_professional", "technology_professional", "engineer", "public_administration", "administrative_support", "sales_marketing", "skilled_trade", "creative_media", "researcher", "manager", "self_employed", "retired", "other", "prefer_not_to_say"];
    public static readonly string[] Industries = ["", "education", "healthcare_social_care", "public_administration", "information_technology_telecom", "finance_insurance", "legal_services", "professional_services", "manufacturing", "construction", "retail_wholesale", "transport_logistics", "hospitality_tourism", "media_culture", "agriculture_food", "energy_utilities", "nonprofit", "other", "prefer_not_to_say"];
}
