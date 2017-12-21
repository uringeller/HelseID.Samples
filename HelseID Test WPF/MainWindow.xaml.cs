﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HelseID.Test.WPF.Common;
using IdentityModel.OidcClient;

namespace HelseID.Test.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        private readonly SystemBrowserManager _browserManager;
        private readonly RequestHandler _requestHandler;
        private LoginResult _loginResult;
        private List<string> _configuredScopes = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            _browserManager = new SystemBrowserManager();

            SetDefaultClientConfiguration();

            _requestHandler = new RequestHandler();            
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Login();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show(ex.StackTrace);
            }
            
        }

        private async Task Login()
        {
            var options = GetClientConfiguration();

            //if (!NetworkHelper.StsIsAvailable(options.Authority))
            //{
            //    MessageBox.Show("Kunne ikke nå adressen:" + options.Authority);
            //}

            var client = new OidcClient(options);            

            try
            {                
                
                var state = await client.PrepareLoginAsync(GetExtraParameters());
                _browserManager.Start(state.StartUrl);

#pragma warning disable 4014
                _requestHandler.Start(client, state, OnLoginSuccess);
#pragma warning restore 4014
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show(e.Message, e.StackTrace);
                throw;
            }

        }

        public object GetExtraParameters()
        {            
            var preselectIdp = PreselectIdpTextBox.Text;

            if (string.IsNullOrEmpty(preselectIdp))
                return null;

            return new { acr_values = preselectIdp, prompt = "Login" };
        }

        private async void OnLoginSuccess(string formData, OidcClient client, AuthorizeState state)
        {
            var result = await client.ProcessResponseAsync(formData, state);

            HandleResult(result);

            Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow == null) return;

                Application.Current.MainWindow.Activate();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            });
        }

        public void HandleResult(LoginResult result)
        {
            if ((result == null) || (result.IsError))
            {
                _loginResult = null;
                AccessTokenClaimsTextBox.Dispatcher.Invoke(() => { AccessTokenClaimsTextBox.Text = string.Empty; });
                IdentityTokenClaimsTextBox.Dispatcher.Invoke(() => { IdentityTokenClaimsTextBox.Text = string.Empty; });
                return;
            }            

            _loginResult = result;

            ShowTokenContent(result.AccessToken, result.IdentityToken);            
        }

        private void ShowTokenContent(string accessToken, string identityToken)
        {
            var accessTokenAsText = accessToken.DecodeToken();
            var idTokenAsText = identityToken.DecodeToken();

            AccessTokenClaimsTextBox.Dispatcher.Invoke(() => { AccessTokenClaimsTextBox.Text = accessTokenAsText; });
            IdentityTokenClaimsTextBox.Dispatcher.Invoke(() => { IdentityTokenClaimsTextBox.Text = idTokenAsText; });
        }

        private void ShowIdTokenRawButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loginResult != null && _loginResult.IdentityToken.IsNotNullOrEmpty())
                ShowTokenViewer(_loginResult.IdentityToken);
            else
                MessageBox.Show("Id Token er ikke tilgjengelig - prøv å logg inn på nytt");
        }

        private void ShowAccessTokenRawButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loginResult != null && _loginResult.AccessToken.IsNotNullOrEmpty())
                ShowTokenViewer(_loginResult.AccessToken);
            else
                MessageBox.Show("Access Token er ikke tilgjengelig - prøv å logg inn på nytt");
        }

        private static void ShowTokenViewer(string content)
        {
            var view = new TokenViewer { Token = content };
            view.ShowDialog();
        }

        private void SetDefaultClientConfiguration()
        {
            //AuthorityTextBox.Text = DefaultClientConfigurationValues.DefaultAuthority;
            ClientIdTextBox.Text = DefaultClientConfigurationValues.DefaultClientId;
            _configuredScopes.Add(DefaultClientConfigurationValues.DefaultScope);
            SecretTextBox.Text = DefaultClientConfigurationValues.DefaultSecret;
            RedirectUrlTextBox.Text = RequestHandler.DefaultUri;

            UpdateScopesTextBox();
        }

        public OidcClientOptions GetClientConfiguration()
        {
            //var authority = AuthorityTextBox.Text;
            var authority = AuthoritiesComboBox.SelectedValue as string;
            var clientId = ClientIdTextBox.Text.Trim();
            var scope = ScopeTextBox.Text.Replace(Environment.NewLine, " ");
            var secret = SecretTextBox.Text;
            var options = new OidcClientOptions()
            {
                Authority = string.IsNullOrEmpty(authority) ? DefaultClientConfigurationValues.DefaultAuthority : authority,
                ClientId = string.IsNullOrEmpty(clientId) ? DefaultClientConfigurationValues.DefaultClientId : clientId,
                RedirectUri = RequestHandler.DefaultUri,
                Scope = string.IsNullOrEmpty(scope) ? DefaultClientConfigurationValues.DefaultScope : scope,
                ClientSecret = string.IsNullOrEmpty(secret) ? DefaultClientConfigurationValues.DefaultSecret : secret,                
            };

            if (UseADFSCheckBox.IsChecked.HasValue && UseADFSCheckBox.IsChecked.Value)
            {
                options.Policy =
                    new Policy()
                    {
                        RequireAccessTokenHash = false //ADFS 2016 spesifikk kode - ikke krev hash for access_token
                    };
            }
            options.BrowserTimeout = TimeSpan.FromSeconds(5);

            options.Flow = OidcClientOptions.AuthenticationFlow.Hybrid;

            return options;
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            //Implement signout? Gir kanskje ikke så mye mening, 
            MessageBox.Show("Lukk nettleservinduet for å logge ut :)");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AuthoritiesComboBox.ItemsSource = Authorities.HelseIdAuthorities;

            SetDefaultScopes();

        }
        private void SetDefaultScopes()
        {
            foreach (var scope in Scopes.DefaultScopes)
            {
                var scopeCheckBox = new CheckBox()
                {
                    Content = scope
                };

                if (_configuredScopes.Contains(scope))
                    scopeCheckBox.IsChecked = true;

                scopeCheckBox.Unchecked += (checkbox, args) =>
                {
                    var box = checkbox as CheckBox;
                    if (box == null) return;

                    var s = box.Content as string;

                    if (!_configuredScopes.Contains(s)) return;

                    _configuredScopes.Remove(s);
                    UpdateScopesTextBox();
                };

                scopeCheckBox.Checked += (checkbox, args) =>
                {

                    var box = checkbox as CheckBox;
                    if (box == null) return;

                    var s = box.Content as string;

                    if (_configuredScopes.Contains(s)) return;

                    _configuredScopes.Add(s);
                    UpdateScopesTextBox();
                };

                ScopesList.Children.Add(scopeCheckBox);
            }

        }
        private void UpdateScopesTextBox()
        {
            ScopeTextBox.Text = string.Join(Environment.NewLine, _configuredScopes);
            ScopeTextBox.Text = string.Join(Environment.NewLine, _configuredScopes);
        }
    }
}
