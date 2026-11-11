using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.Controls;

public partial class AccountSettingsView : UserControl
{
    private readonly AccountSessionCoordinator _session;
    private bool _updating, _attached, _dirty;
    private long? _draftUser;
    private readonly System.Windows.Threading.DispatcherTimer _cooldown = new() { Interval = TimeSpan.FromSeconds(1) };
    public event EventHandler? IdentityChanged;
    public string CardIdentity => _session.IsSignedIn ? _session.Profile?.DisplayName ?? _session.Profile?.Email ?? SettingsText.Get("Category.profile") : SettingsText.Get("Category.profile");
    public string CardStatus => _session.IsSignedIn ? _session.Profile?.Email ?? CopyStatus() : CopyStatus();
    private sealed record Option(string Value, string Label);
    public AccountSettingsView(AccountSessionCoordinator session)
    {
        _session = session; InitializeComponent();
        Loaded += OnLoaded; Unloaded += OnUnloaded;
        LoginButton.Click += async (_, _) => await Submit();
        LoginPassword.PreviewKeyDown += async (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; await Submit(); } };
        AccessMode.SelectionChanged += (_, _) => Render();
        ReloadAccount.Click += async (_, _) =>
        {
            if (_dirty && MessageBox.Show(Window.GetWindow(this), SettingsText.Get("AccountDiscardDraft"), "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _draftUser = null; _dirty = false; await (_session.IsSignedIn ? _session.ReloadProfileAsync() : _session.InitializeAsync());
        };
        ResendButton.Click += async (_, _) => await _session.ResendAsync(_session.IsSignedIn ? _session.Profile?.Email ?? "" : LoginEmail.Text.Trim());
        UnlockAccount.Click += async (_, _) => await _session.UnlockAsync();
        HelloCheck.Click += async (_, _) => { var enabled = HelloCheck.IsChecked == true; Render(); await _session.SetHelloAsync(enabled); };
        SaveProfile.Click += async (_, _) => { if (await _session.SaveProfileAsync(ProfileName.Text, Age.SelectedValue as string, Profession.SelectedValue as string, Industry.SelectedValue as string, Personalization.IsChecked == true)) { _dirty = false; FillDraft(); } };
        ChangePassword.Click += async (_, _) => { var a = CurrentPassword.Password; var b = NewPassword.Password; var c = ConfirmPassword.Password; ClearPasswords(); await _session.ChangePasswordAsync(a,b,c); };
        ChangeEmail.Click += async (_, _) => { var password = EmailPassword.Password; EmailPassword.Clear(); await _session.ChangeEmailAsync(NewEmail.Text.Trim(), ConfirmEmail.Text.Trim(), password); };
        CancelEmail.Click += async (_, _) => await _session.CancelEmailChangeAsync();
        LogoutCurrent.Click += async (_, _) => { ClearPasswords(); await _session.LogoutAsync(false); };
        LogoutAll.Click += async (_, _) =>
        {
            if (MessageBox.Show(Window.GetWindow(this), SettingsText.Get("AccountAllConfirm"), "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            ClearPasswords(); await _session.LogoutAsync(true);
        };
        ProfileName.TextChanged += (_, _) => { if (!_updating) _dirty = true; };
        foreach (var picker in new[] { Age, Profession, Industry }) picker.SelectionChanged += (_, _) => { if (!_updating) _dirty = true; };
        Personalization.Click += (_, _) => _dirty = true;
        SetOptions(); Render();
        _cooldown.Tick += (_, _) => ResendButton.IsEnabled = !_session.Busy && _session.Config.Supports("email_verification_v1_issuance") && DateTimeOffset.UtcNow >= _session.ResendAfter;
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_attached) return; _attached = true;
        _session.PropertyChanged += Changed; SettingsText.Changed += CopyChanged;
        _cooldown.Start();
        await _session.InitializeAsync();
    }
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_attached) return; _attached = false;
        _session.PropertyChanged -= Changed; SettingsText.Changed -= CopyChanged;
        _cooldown.Stop();
        ClearPasswords(); _session.Suspend();
    }
    private void Changed(object? sender, PropertyChangedEventArgs e)
    { if (!_attached) return; if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => { if (_attached) Render(); }); return; } Render(); }
    private void CopyChanged(object? sender, EventArgs e) { if (!_attached) return; SetOptions(); Render(); }
    private void SetOptions()
    {
        _updating = true;
        try
        {
            var mode = AccessMode.SelectedValue as string ?? "login";
            AccessMode.ItemsSource = new[] { new Option("login", SettingsText.Get("AccountLogin")), new Option("register", SettingsText.Get("AccountRegister")) }; AccessMode.SelectedValue = mode;
            SetCatalog(Age, AccountCatalogs.Ages); SetCatalog(Profession, AccountCatalogs.Professions); SetCatalog(Industry, AccountCatalogs.Industries);
        }
        finally { _updating = false; }
    }
    private static void SetCatalog(ComboBox picker, string[] ids)
    {
        var selected = picker.SelectedValue as string ?? "";
        picker.ItemsSource = ids.Append(selected).Distinct().Select(id => new Option(id, id.Length == 0 ? SettingsText.Get("AccountOptional") : SettingsText.Keys.Contains("AccountOption." + id) ? SettingsText.Get("AccountOption." + id) : id)).ToArray();
        picker.SelectedValue = selected;
    }
    private string CopyStatus() => SettingsText.Get("AccountStatus." + (SettingsText.Keys.Contains("AccountStatus." + _session.StatusCode) ? _session.StatusCode : "server_error"));
    private void Render()
    {
        if (_updating) return; _updating = true;
        try
        {
            Identity.Text = CardIdentity; AccountStatus.Text = CopyStatus();
            Fields.IsEnabled = !_session.Busy; ReloadAccount.IsEnabled = !_session.Busy;
            UnlockAccount.Visibility = _session.HelloEnabled && !_session.IsSignedIn ? Visibility.Visible : Visibility.Collapsed;
            UnlockAccount.IsEnabled = !_session.Busy && _session.HelloAvailable;
            Anonymous.Visibility = _session.IsSignedIn ? Visibility.Collapsed : Visibility.Visible;
            Authenticated.Visibility = _session.IsSignedIn ? Visibility.Visible : Visibility.Collapsed;
            LogoutCurrent.Visibility = Visibility.Visible;
            var register = AccessMode.SelectedValue as string == "register";
            RegistrationConfirmationRow.Visibility = register ? Visibility.Visible : Visibility.Collapsed;
            VerificationRow.Visibility = _session.Config.Supports("email_verification_v1_issuance") &&
                (_session.Profile is { EmailVerified: false } || _session.StatusCode is "verification_pending" or "forbidden" || DateTimeOffset.UtcNow < _session.ResendAfter)
                ? Visibility.Visible : Visibility.Collapsed;
            LoginButton.Content = SettingsText.Get(register ? "AccountRegister" : "AccountLogin");
            LoginButton.IsEnabled = !register || _session.Config.Supports("email_verification_v1_issuance");
            ResendButton.IsEnabled = _session.Config.Supports("email_verification_v1_issuance") && DateTimeOffset.UtcNow >= _session.ResendAfter;
            HelloCheck.IsChecked = _session.HelloEnabled; HelloCheck.IsEnabled = _session.HasRecovery && (_session.HelloAvailable || _session.HelloEnabled);
            PersonalFields.IsEnabled = _session.Personalization is not null;
            ChangeEmail.IsEnabled = CancelEmail.IsEnabled = _session.Config.Supports("email_change_v1");
            EmailChangeRow.Visibility = _session.Config.Supports("email_change_v1") ? Visibility.Visible : Visibility.Collapsed;
            VerifiedEmail.Text = _session.Profile is { } p ? p.Email + " · " + SettingsText.Get(p.EmailVerified ? "AccountVerified" : "AccountUnverified") : SettingsText.Get("AccountUnavailable");
            Subscription.Text = _session.Entitlement is { } ent ? ent.Product + " · " + ent.Tier + " · " + ent.Status + (ent.ValidUntil is { } until ? " · " + until.ToLocalTime().ToString("g") : "") : SettingsText.Get("AccountUnavailable");
            if (_session.IsSignedIn && _session.Profile is { } profile && _draftUser != profile.UserId) FillDraft();
            if (!_session.IsSignedIn) { if (_draftUser is not null) ClearPasswords(); _draftUser = null; }
        }
        finally { _updating = false; IdentityChanged?.Invoke(this, EventArgs.Empty); }
    }
    private void FillDraft()
    {
        var old = _updating; _updating = true;
        try
        {
            var p = _session.Personalization; _draftUser = _session.Profile?.UserId;
            ProfileName.Text = _session.Profile?.DisplayName ?? "";
            foreach (var item in new[] { (Age, p?.AgeBand ?? "", AccountCatalogs.Ages), (Profession, p?.ProfessionCode ?? "", AccountCatalogs.Professions), (Industry, p?.IndustryCode ?? "", AccountCatalogs.Industries) })
            { item.Item1.SelectedValue = item.Item2; if (item.Item1.SelectedValue is null) { item.Item1.ItemsSource = item.Item3.Append(item.Item2).Distinct().Select(id => new Option(id, id)).ToArray(); item.Item1.SelectedValue = item.Item2; } }
            Personalization.IsChecked = p?.PersonalizationEnabled == true; _dirty = false;
        }
        finally { _updating = old; }
    }
    private async Task Submit()
    {
        var email = LoginEmail.Text.Trim(); var password = LoginPassword.Password; var confirm = LoginConfirm.Password; ClearPasswords();
        if (AccessMode.SelectedValue as string == "register") await _session.RegisterAsync(email,password,confirm);
        else await _session.LoginAsync(email,password);
    }
    private void ClearPasswords() { foreach (var password in new[] { LoginPassword, LoginConfirm, CurrentPassword, NewPassword, ConfirmPassword, EmailPassword }) password.Clear(); }
}
