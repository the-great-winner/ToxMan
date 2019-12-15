using MahApps.Metro.Controls;
using SharpTox.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ToxMan
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
          InitializeComponent();

          _lvContacts.SelectionChanged += _lvContacts_SelectionChanged;
            _txtContactAdd.KeyDown += _txtContactAdd_KeyDown;
        }
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ToxUtils.LoadMyself();
            Task.Run(RunTox);
        }        
        private void RunTox()
        {
            Nodes = ToxUtils.UpdateNodeList() ?? Nodes;

            var toxBotID = ToxUtils.Me.Secret ??
                "9a71a67e24a6ab1205a027c538f075709e5a2b88075f58e4ddaa0234c7aa4bb9";
            ToxOptions options = new ToxOptions(true, true);

           _tox = new Tox(options, new ToxKey(ToxKeyType.Secret, toxBotID) );
            _tox.OnFriendRequestReceived += tox_OnFriendRequestReceived;
            _tox.OnFriendMessageReceived += tox_OnFriendMessageReceived;

            foreach (ToxNode node in Nodes)
                try { _tox.Bootstrap(node); } catch { }

            _tox.Name = ToxUtils.Me.Name;
            _tox.StatusMessage = ToxUtils.Me.StatusMsg; // 
            _tox.Start();

            string id = _tox.Id.ToString();
            Console.WriteLine("ID: {0}", id);

            ToxUtils.LoadFriendIds(); 
      
            if(ToxUtils.Friends.All != null)
            foreach (var f in ToxUtils.Friends.All)
            {
                _tox.AddFriendNoRequest(
                    new ToxKey(ToxKeyType.Public, f.ToxID),
                    out ToxErrorFriendAdd aa);
            }

            Dispatcher.Invoke( OnNewTox );

            _tox_evt.WaitOne();
        }

        private void OnNewTox()
        {
            _panelCL.Visibility = Visibility.Visible;
            this._lblToxID.Text = "Connecting...";

            WaitForConnect();

            _lvContacts.Items.Clear();
            foreach(var i in _tox.Friends)
            {
                var k = _tox.GetFriendPublicKey(i);
                if(!k.GetBytes().All(b => b == 0x00))
                _lvContacts.Items.Add(new ListViewItem { Content = k.GetString()});
            }
        }

        private void WaitForConnect()
        {
            Task.Run(() =>
            {
                try
                {
                    while (!_tox.IsConnected)
                        Thread.Sleep(100);
                    Dispatcher.Invoke(() => 
                    this._lblToxID.Text = "Connected: " + Leave(8, 6, _tox.Id.ToString()));
                }
                catch { }
            });
        }

        private string Leave(int v1, int v2, string v3)
        {
            int isfx = v3.Length - v2;
            string prefix = v3.Substring(0, v1);
            string suffix = (isfx < 3)? "" : v3.Substring(isfx, v2);
            return $"{prefix}...{suffix}";
        }

        private Tox _tox;

        //check https://nodes.tox.chat/ for an up-to-date list of nodes
        static ToxNode[] Nodes = new ToxNode[]
        { 
         new ToxNode("5.79.75.37", 33445, new ToxKey(ToxKeyType.Public,
           "D527E5847F8330D628DAB1814F0A422F6DC9D0A300E6C357634EE2DA88C35463")),
         new ToxNode("46.229.52.198", 33445, new ToxKey(ToxKeyType.Public, 
           "813C8F4187833EF0655B10F7752141A352248462A567529A38B6BBF73E979307")),
        };

        ManualResetEvent _tox_evt = new ManualResetEvent(false);

        void tox_OnFriendMessageReceived(object sender, ToxEventArgs.FriendMessageEventArgs e)
        {
            //get the name associated with the friendnumber
            string name = _tox.GetFriendName(e.FriendNumber);
            string g = name + ": " + e.Message;
            ToxUtils.LogChat(_tox.GetFriendPublicKey(e.FriendNumber).GetString(), g);
            Dispatcher.Invoke(() => this._txbConsole.Text +=g +"\r\n");

            //print the message to the console
            // Console.WriteLine("<{0}> {1}", name, e.Message);
        }

         void tox_OnFriendRequestReceived(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            //automatically accept every friend request we receive
            _tox.AddFriendNoRequest(e.PublicKey);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ToxUtils.SaveAllCfgFiles();
            _tox.Dispose();
            _tox_evt.Set();
        }

        private void _txtContactAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_ContactApproveClick(null, null);

        }

        private void _lvContacts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var i = e.AddedItems;

            ListViewItem ii = i[0] as ListViewItem;

            _txbConsole.Text = ToxUtils.ChatHistory(ii.Content.ToString());
            
        }
        private void Button_ContactAddClick(object sender, RoutedEventArgs e)
        {
            _btContactApprove.Visibility = Visibility.Visible;
            _txtContactAdd.Visibility = Visibility.Visible;
            _btContactAdd.Visibility = Visibility.Collapsed;
            _txtContactAdd.Focus();
            // ToxUtils.AddFriend();
        }
        private void Button_ContactApproveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var k = _txtContactAdd.Text.Trim();
                if (k.Length == 0)
                {
                    RestoreAddButton();
                    return;
                    // throw new Exception();
                }

                ToxKey newKey = new ToxKey(ToxKeyType.Public, k);
                if (!ToxUtils.FriendAdded(newKey))
                {
                    _tox.AddFriendNoRequest(newKey, out ToxErrorFriendAdd aa);
                    if (aa == ToxErrorFriendAdd.Ok)
                    {
                        ToxUtils.AddFriend(newKey);
                        _lvContacts.Items.Add(new ListViewItem { Content = newKey.GetString() });
                    }
                    else
                        throw new Exception();
                }
            }
            catch 
            {
                _txtContactAdd.Foreground = Brushes.Red;
                return; 
            }
            RestoreAddButton();
        }

        private void RestoreAddButton()
        {
            _txtContactAdd.Foreground = Brushes.Lime;
            _btContactApprove.Visibility = Visibility.Collapsed;
            _btContactAdd.Visibility = Visibility.Visible;
            _txtContactAdd.Visibility = Visibility.Collapsed;
        }

        private void _mm_show_nodes_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ToxMan.ToxUtils.PathToxNodes);
        }

        private void _mm_show_friends_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ToxMan.ToxUtils.PathToxFriends);
        }

        private void _txtContactAdd_LostFocus(object sender, RoutedEventArgs e)
        {
            RestoreAddButton();
        }

        private void _txtEnterMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
           var  ii = _lvContacts.SelectedItem as ListViewItem;
            if (ii == null)
                return;
           var toSend = _txtEnterMessage.Text.Trim();

           var  k =  ii.Content.ToString();
            _tox.SendMessage(_tox.GetFriendByPublicKey(new ToxKey(ToxKeyType.Public, k)), toSend,
                ToxMessageType.Message, out ToxErrorSendMessage err);
            if(err == ToxErrorSendMessage.FriendNotConnected || err == ToxErrorSendMessage.Ok)
            {
                if (err == ToxErrorSendMessage.FriendNotConnected)
                    toSend  = "[Wait]\t" + toSend;
                ToxUtils.LogChat(k, toSend);
                _txbConsole.Text += toSend + "\r\n";
                _txtEnterMessage.Text = "";
                _txtEnterMessage.Focus();
            }
        }

        private void _linkToxID_Click(object sender, RoutedEventArgs e)
        {
            if (_tox.IsConnected)
            {
                _lblToxID.Text = "Copied " + _tox.Id.ToString();
                Clipboard.SetText(_tox.Id.ToString());
                Task.Run(() =>
                 {
                     Thread.Sleep(7000);
                     WaitForConnect();
                 }
                );
            }
        }

        private void _mm_show_profile_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ToxMan.ToxUtils.PathProfile);
        }
    }
}
