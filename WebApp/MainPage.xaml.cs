using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Json;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace RssHybrid
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SQLiteConnection db;
        private Bridge bridge;

        public MainPage()
        {
            this.InitializeComponent();

            bridge = new Bridge();

            bridge.AddAction("getFeeds", (brigeParams) => GetFeedsAction(brigeParams));
            bridge.AddAction("getAllNews", (brigeParams) => GetAllNewsAction(brigeParams));

            db = new SQLiteConnection("Data Source=Assets/database.sqlite;Version=3;");
            db.Open();

            webView.NavigationStarting += NavigationStarting;
            webView.ScriptNotify += ScriptNotify;

            webView.Navigate(new Uri(@"ms-appx-web:///Web/index.html"));
        }

        private void NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Debug.WriteLine(args.Uri.ToString());            
        }

        private void ScriptNotify(object sender, NotifyEventArgs e)
        {
            Debug.WriteLine(e.Value.ToString());
            bridge.Call(e.Value.ToString());
        }

        private async void BridgeCallback(string id, string data)
        {
            string[] args = { id, data };
            string returnValue = await webView.InvokeScriptAsync("_bridgeCallback", args);
        }

        private void GetFeedsAction(BridgeParameters brigeParams)
        {
            string data = GetFeeds();
            BridgeCallback(brigeParams.Id, data);
        }

        private void GetAllNewsAction(BridgeParameters brigeParams)
        {
            int offset = Int32.Parse(brigeParams.Parameters["from"]);
            int limit = Int32.Parse(brigeParams.Parameters["to"]);

            string data = GetAllNews(offset, limit);
            BridgeCallback(brigeParams.Id, data);
        }

        private string GetFeeds()
        {
            string sql = "SELECT f.*, COUNT(e.id) AS total, 0 AS count FROM feeds f LEFT JOIN entries e ON e.feed_id = f.id WHERE f.deleted = 0 GROUP BY f.id";
            SQLiteCommand command = new SQLiteCommand(sql, db);

            SQLiteDataReader reader = command.ExecuteReader();
            
            var r = Serialize(reader);
            string json = JsonConvert.SerializeObject(r, Formatting.Indented);
          
            return json;
        }

        private string GetAllNews(int offset, int limit)
        {
            string sql = "SELECT * FROM entries ORDER BY id DESC LIMIT @limit OFFSET @offset";
            SQLiteCommand command = new SQLiteCommand(sql, db);

            var limitParam = new SQLiteParameter("@limit", limit);
            var offsetParam = new SQLiteParameter("@offset", offset);

            command.Parameters.Add(limitParam);
            command.Parameters.Add(offsetParam);

            SQLiteDataReader reader = command.ExecuteReader();

            var r = Serialize(reader);
            string json = JsonConvert.SerializeObject(r, Formatting.Indented);

            return json;
        }

        public IEnumerable<Dictionary<string, object>> Serialize(SQLiteDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                cols.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                results.Add(SerializeRow(cols, reader));
            }

            return results;
        }

        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SQLiteDataReader reader)
        {
            var result = new Dictionary<string, object>();

            foreach (var col in cols)
            {
                result.Add(col, reader[col]);
            }

            return result;
        }
    }
}
