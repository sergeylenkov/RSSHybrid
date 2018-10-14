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
using CodeHollow.FeedReader;
using Windows.Storage;
using System.Threading.Tasks;

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
            bridge.AddAction("getNews", (brigeParams) => GetNewsAction(brigeParams));
            bridge.AddAction("getTotalCount", (brigeParams) => GetTotalCountAction(brigeParams));

            string path = ApplicationData.Current.LocalFolder.Path + "\\database.sqlite";
            CopyDatabase(path);

            db = new SQLiteConnection("Data Source=" + path + ";Version=3;");
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
            //Debug.WriteLine(e.Value.ToString());
            bridge.Call(e.Value.ToString());
        }

        private async void BridgeCallback(string id, string data)
        {
            Debug.WriteLine("BridgeCallback {0}", id);
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

        private async void GetNewsAction(BridgeParameters brigeParams)
        {
            string data = await UpdateFeeds();            
            Debug.WriteLine("GetNewsAction end");
            BridgeCallback(brigeParams.Id, data);
        }

        private void GetTotalCountAction(BridgeParameters brigeParams)
        {
            string data = GetTotalCount();
            BridgeCallback(brigeParams.Id, data);
        }

        private string GetFeeds()
        {
            string sql = "SELECT f.*, COUNT(e.id) AS total, 0 AS count FROM feeds f LEFT JOIN entries e ON e.feed_id = f.id WHERE f.deleted = 0 GROUP BY f.id";
            SQLiteCommand command = new SQLiteCommand(sql, db);

            SQLiteDataReader reader = command.ExecuteReader();
            
            var r = Serialize(reader);
            return JsonConvert.SerializeObject(r, Formatting.Indented);
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
            return JsonConvert.SerializeObject(r, Formatting.Indented);
        }

        private async Task<string> UpdateFeeds()
        {
            await Task.Run(() => {
                string updateSql = "UPDATE entries SET viewed = @viewed";
                SQLiteCommand updateCommand = new SQLiteCommand(updateSql, db);

                updateCommand.Parameters.Add(new SQLiteParameter("@viewed", true));

                updateCommand.ExecuteScalar();

                string sql = "SELECT id, rss FROM feeds";
                SQLiteCommand command = new SQLiteCommand(sql, db);

                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var task = FeedReader.ReadAsync(reader.GetString(1));

                    var feedId = reader.GetInt32(0);
                    Debug.WriteLine("Parse feed {0}", feedId);
                    foreach (var item in task.Result.Items)
                    {
                        string existsSql = "SELECT id FROM entries WHERE feed_id = @feedId AND guid = @guid";
                        SQLiteCommand existsCommand = new SQLiteCommand(existsSql, db);

                        existsCommand.Parameters.Add(new SQLiteParameter("@feedId", feedId));
                        existsCommand.Parameters.Add(new SQLiteParameter("@guid", item.Id));

                        if (existsCommand.ExecuteScalar() == null)
                        {
                            string insertSql = "INSERT INTO entries(feed_id, guid, link, title, description, read, viewed, favorite, date) VALUES(@feedId, @guid, @link, @title, @description, @read, @viewed, @favorite, @date)";
                            SQLiteCommand insertCommand = new SQLiteCommand(insertSql, db);

                            insertCommand.Parameters.Add(new SQLiteParameter("@feedId", feedId));
                            insertCommand.Parameters.Add(new SQLiteParameter("@guid", item.Id));
                            insertCommand.Parameters.Add(new SQLiteParameter("@link", item.Link));
                            insertCommand.Parameters.Add(new SQLiteParameter("@title", item.Title));
                            insertCommand.Parameters.Add(new SQLiteParameter("@description", item.Description));
                            insertCommand.Parameters.Add(new SQLiteParameter("@read", false));
                            insertCommand.Parameters.Add(new SQLiteParameter("@viewed", false));
                            insertCommand.Parameters.Add(new SQLiteParameter("@favorite", false));
                            insertCommand.Parameters.Add(new SQLiteParameter("@date", item.PublishingDate));

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            });            

            return GetNews();
        }

        private string GetNews()
        {
            string sql = "SELECT * FROM entries WHERE viewed = @viewed ORDER BY id DESC LIMIT 30";

            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.Parameters.Add(new SQLiteParameter("@viewed", true));

            SQLiteDataReader reader = command.ExecuteReader();

            var r = Serialize(reader);
            return JsonConvert.SerializeObject(r, Formatting.Indented);
        }

        private string GetTotalCount()
        {
            string sql = "SELECT COUNT(*) FROM entries";
            SQLiteCommand command = new SQLiteCommand(sql, db);

            return command.ExecuteScalar().ToString();
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

        private async void CopyDatabase(string path)
        {
            if (!File.Exists(path))
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/database.sqlite"));
                await file.CopyAsync(ApplicationData.Current.LocalFolder, "database.sqlite");
            }
        }
    }
}
