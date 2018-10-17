using Microsoft.Toolkit.Uwp.Notifications;
using RSSHybrid;
using System;
using System.Diagnostics;
using Windows.Data.Xml.Dom;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;

namespace RssHybrid
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {        
        private Bridge bridge;

        public MainPage()
        {
            this.InitializeComponent();

            bridge = new Bridge();

            bridge.AddAction("getFeeds", (brigeParams) => GetFeedsAction(brigeParams));
            bridge.AddAction("getAllNews", (brigeParams) => GetAllNewsAction(brigeParams));
            bridge.AddAction("updateFeeds", (brigeParams) => UpdateFeedsAction(brigeParams));
            bridge.AddAction("getTotalCount", (brigeParams) => GetTotalCountAction(brigeParams));

            webView.NavigationStarting += NavigationStarting;
            webView.ScriptNotify += ScriptNotify;

            webView.Navigate(new Uri(@"ms-appx-web:///Web/index.html"));

            SystemNavigationManager.GetForCurrentView().BackRequested += GoBack;

            SetBadgeNumber(DataAccess.GetUnviewedCount())
;        }

        private void NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Debug.WriteLine(args.Uri.ToString());

            if (args.Uri.ToString() != "ms-appx-web:///Web/index.html")
            {
                ShowBackButton();
            }
        }

        private void ScriptNotify(object sender, NotifyEventArgs e)
        {
            //Debug.WriteLine(e.Value.ToString());
            bridge.Call(e.Value.ToString());
        }

        private async void BridgeCallback(string id, string data)
        {
            Debug.WriteLine("BridgeCallback {0}", (object)id);
            string[] args = { id, data };
            string returnValue = await webView.InvokeScriptAsync("_bridgeCallback", args);
        }

        private void GetFeedsAction(BridgeParameters brigeParams)
        {
            string data = DataAccess.GetFeeds();
            BridgeCallback(brigeParams.Id, data);
        }        

        private void GetAllNewsAction(BridgeParameters brigeParams)
        {
            int offset = Int32.Parse(brigeParams.Parameters["from"]);
            int limit = Int32.Parse(brigeParams.Parameters["to"]);

            string data = DataAccess.GetAllNews(offset, limit);
            BridgeCallback(brigeParams.Id, data);
        }

        private async void UpdateFeedsAction(BridgeParameters brigeParams)
        {
            await DataAccess.UpdateFeeds();
            string data = DataAccess.GetUnviewedNews();

            int count = DataAccess.GetUnviewedCount();

            if (count > 0)
            {
                ShowNotification(count);
            }

            SetBadgeNumber(count);

            BridgeCallback(brigeParams.Id, data);
        }

        private void GetTotalCountAction(BridgeParameters brigeParams)
        {
            string data = DataAccess.GetTotalCount().ToString();
            BridgeCallback(brigeParams.Id, data);
        }

        private void ShowBackButton()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private void GoBack(object e, BackRequestedEventArgs args)
        {
            if (webView.CanGoBack)
            {
                webView.GoBack();
            }

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private void ShowNotification(int count)
        {
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = "Good news everyone!"
                        },

                        new AdaptiveText()
                        {
                            Text = String.Format("{0} new entries", count)
                        }
                    }                   
                }
            };

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now;

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void SetBadgeNumber(int count)
        {
            XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
         
            XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
            badgeElement.SetAttribute("value", count.ToString());
            
            BadgeNotification badge = new BadgeNotification(badgeXml);
            
            BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            badgeUpdater.Update(badge);
        }
    }
}
