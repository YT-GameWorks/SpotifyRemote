﻿//------------------------------------------------------------------------------
// <copyright file="SpotifyRemotePlayListWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


#define LOCAL_PLAY

namespace VSIXSpotifyRemote
{
    using SpotifyAPI.Web.Enums;
    using SpotifyAPI.Web.Models;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;
    using System;
    using VSIXSpotifyRemote.PlayList;
    using System.Timers;
    using System.Diagnostics;

    namespace PlayList {
        public class SpotListViewItem
        {
            public Label Name { get; set; }

            public Label Open { get; set; }

            public int playListId { get; set; }
        }

        public class SpotPlayListLabel : Label
        {
            public int playlistId { get; set; }
            public string spotifyTrackId { get; set; }
        }
    }


   

    /// <summary>
    /// Interaction logic for SpotifyRemotePlayListWindowControl.
    /// </summary>
    public partial class SpotifyRemotePlayListWindowControl : UserControl
    {
        enum ListViewMode
        {
            kPlayList = 0,
            kTrackList = 1
        }


        private static int kMAX_PLAYLISTS = 100;
        private static int kMAX_TRACKS = 100;

        Brush foregroundLabelBrush;
        int selectedIndex = -1;
        string userId;
        ListViewMode lvm = ListViewMode.kPlayList;

        private List<string> playListNames;
        private List<string> playListUser;
        private List<string> playListId;
        private List<string> playListUri;
        private List<PlaylistTrack> listTracksFromPL;


        System.Windows.Media.Color foregroundColor = Color.FromRgb(226, 204, 129);


        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyRemotePlayListWindowControl"/> class.
        /// </summary>
        public SpotifyRemotePlayListWindowControl()
        {

            this.InitializeComponent();

            playListNames = new List<string>(kMAX_PLAYLISTS);
            playListUser = new List<string>(kMAX_PLAYLISTS);
            playListId = new List<string>(kMAX_PLAYLISTS);
            playListUri= new List<string>(kMAX_PLAYLISTS);
            listTracksFromPL = new List<PlaylistTrack>(kMAX_TRACKS);

            Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
            lvm = ListViewMode.kPlayList;


        }

        void SetListViewColors(System.Windows.Media.Color color)
        {
            //SolidColorBrush brushCol = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            SolidColorBrush brushCol = new SolidColorBrush(color);
            for (int i = 0; i < listView.Items.Count; i++)
            {
                (((SpotListViewItem)listView.Items[i]).Name).Foreground = brushCol;
            }
        }

        private void UpdateUIColors()
        {
            var defaultBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var defaultForeground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);

            System.Drawing.Color c = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

            SolidColorBrush backgroundCol = new SolidColorBrush(ThemeHelper.ToMediaColor(defaultBackground));
            switch (ThemeHelper.GetTheme())
            {
                case ThemeHelper.eVSTheme.kDark:
                    foregroundColor = Color.FromRgb(226, 204, 129);
                    WindowTitle.Foreground = new SolidColorBrush(Color.FromRgb(186, 255, 171));
                    WindowTitle.Background = new SolidColorBrush(ThemeHelper.ToMediaColor(defaultBackground));
                    Background = backgroundCol;
                    listView.Background = backgroundCol;
                    break;
                case ThemeHelper.eVSTheme.kBlue:
                    foregroundColor = Color.FromRgb(0, 0, 0);
                    WindowTitle.Foreground = new SolidColorBrush(Color.FromRgb(83, 114, 76));
                    WindowTitle.Background = new SolidColorBrush(ThemeHelper.ToMediaColor(defaultBackground));
                    Background = backgroundCol;
                    listView.Background = backgroundCol;
                    break;
                case ThemeHelper.eVSTheme.kLight:
                    foregroundColor = Color.FromRgb(0, 0, 0);
                    WindowTitle.Foreground = new SolidColorBrush(Color.FromRgb(83, 114, 76));
                    WindowTitle.Background = backgroundCol;
                    Background = backgroundCol;
                    listView.Background = backgroundCol;
                    break;
                case ThemeHelper.eVSTheme.kUnknown:
                    //break;
                default:
                    byte a = defaultForeground.A;
                    byte r = defaultForeground.R;
                    byte g = defaultForeground.G;
                    byte b = defaultForeground.B;
                    foregroundColor = Color.FromArgb(a, r, g, b);
                    WindowTitle.Foreground = new SolidColorBrush(foregroundColor);
                    WindowTitle.Background = new SolidColorBrush(ThemeHelper.ToMediaColor(defaultBackground));
                    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Spotify extension couldnt detect color scheme. \nWould you be so kind to file a bug report?")));
                    break;
            }

            foregroundLabelBrush = new SolidColorBrush( foregroundColor);

            SetListViewColors(foregroundColor);
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {

            UpdateUIColors();
            //throw new System.NotImplementedException();
        }

        private void MyToolWindow_Loaded(object sender, RoutedEventArgs e)
        {

            lvm = ListViewMode.kPlayList;
            UpdateUIColors();

            if (!Command1Package.isAuthenticated())
            {
                Command1Package.AuthenticateSpotifyWeb();
            }

            
            if(Command1Package.spotWeb != null)
            {
                RetreivePlayListsFromWeb();
                ShowPlayListsInView();
            }
            else
            {
                MessageBox.Show("Sorry, authentication failed, its needed for this feature.");
            }


            




            listView.Height = this.ActualHeight;// - WindowTitle.ActualHeight;

          
        }

        void RetreivePlayListsFromWeb()
        {

            if(Command1Package.spotWeb == null)
            {
                MessageBox.Show("Cant retrieve Play list, not authenticated.");
                return;
            }

            PrivateProfile privProfile = Command1Package.spotWeb.GetPrivateProfile();
            userId = privProfile.Id;
            Paging<SimplePlaylist> pPlayLists = Command1Package.spotWeb.GetUserPlaylists(privProfile.Id, kMAX_PLAYLISTS, 0);

            for (int i = 0; i < pPlayLists.Items.Count; i++)
            {
                playListUser.Add(pPlayLists.Items[i].Owner.Id);
                playListNames.Add(pPlayLists.Items[i].Name);
                playListId.Add(pPlayLists.Items[i].Id);
                playListUri.Add(pPlayLists.Items[i].Uri);
            }

        }

        bool RetreiveTracksFromWeb(string playListId, string userId)
        {
            if (Command1Package.spotWeb == null)
            {
                MessageBox.Show("Cant retrieve Play list, not authenticated.");
                return false;
            }

            listTracksFromPL.Clear();
            Paging<PlaylistTrack> pagingTracks = Command1Package.spotWeb.GetPlaylistTracks(userId, playListId, "", kMAX_TRACKS, 0, "");
            if(pagingTracks.Items == null)
            {
#if DEBUG
                if (pagingTracks.HasError())
                {
                    Dispatcher.BeginInvoke(new System.Action(() => MessageBox.Show(pagingTracks.Error.Message, "Spotify debug error message")));
                    Debug.WriteLine(pagingTracks.Error.Status + ":" + pagingTracks.Error.Message);
                }
#endif
                return false;
            }
            listTracksFromPL.AddRange(pagingTracks.Items);
            return true;
            

        }

        void ShowPlayListsInView()
        {
            lvm = ListViewMode.kPlayList;
            ReturnPlaylists.Visibility = Visibility.Collapsed;
            LabelPlayPlayList.Visibility = Visibility.Collapsed;
            RecalculateListView();


            //var gridView = new GridView();
            //listView.View = gridView;
            for (int i = 0; i < playListNames.Count; i++)
            {

                

                Label l = new Label();
                l.Foreground = new SolidColorBrush(foregroundColor);
                l.Content = playListNames[i];
                l.FontFamily = new FontFamily("Segoe UI Light");

                //gridView.Columns

                PlayList.SpotPlayListLabel openLabel = new PlayList.SpotPlayListLabel();
                openLabel.Content = "Open";
                
                openLabel.MouseUp += OpenLabelClicked;
                openLabel.playlistId = i;

                listView.Items.Add(new PlayList.SpotListViewItem { Name = l, Open = openLabel, playListId = i });
                
            }
            
            //foregroundLabelBrush = ((Label)((PlayList.SpotListViewItem)listView.Items[0]).Name).Foreground;


        }

        void ShowTracksInListView()
        {
            lvm = ListViewMode.kTrackList;
            ReturnPlaylists.Visibility = Visibility.Visible;
            LabelPlayPlayList.Visibility = Visibility.Visible;
            RecalculateListView();


            //var gridView = new GridView();
            //listView.View = gridView;
            for (int i = 0; i < listTracksFromPL.Count; i++)
            {



                Label l = new Label();
                l.Foreground = foregroundLabelBrush;
                l.Content = listTracksFromPL[i].Track.Name;
                l.FontFamily = new FontFamily("Segoe UI Light");

                //gridView.Columns

                PlayList.SpotPlayListLabel openLabel = new PlayList.SpotPlayListLabel();
                openLabel.Content = "Play";

                openLabel.MouseUp += OpenLabelClicked;
                openLabel.playlistId = 0;
                openLabel.spotifyTrackId = listTracksFromPL[i].Track.Uri;

                listView.Items.Add(new PlayList.SpotListViewItem { Name = l, Open = openLabel, playListId = i });

            }

            foregroundLabelBrush = new SolidColorBrush(foregroundColor);


        }

        private void OpenLabelClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            if (Command1Package.spotWeb == null)
            {
                MessageBox.Show("Cant retrieve Play list, not authenticated.");
                return;
            }

            if (lvm == ListViewMode.kPlayList)
            {
                
                bool successful = RetreiveTracksFromWeb(playListId[((PlayList.SpotPlayListLabel)e.Source).playlistId], playListUser[((PlayList.SpotPlayListLabel)e.Source).playlistId]);
                if(successful)
                {
                    listView.Items.Clear();
                    ShowTracksInListView();
                }
                else
                {
                    Dispatcher.BeginInvoke(new System.Action(() => MessageBox.Show("Sorry, can't show you this playlist.\nTry again later.")));
                }
            }
            else
            {
                Command1Package.spotClient.PlayURL(((PlayList.SpotPlayListLabel)e.Source).spotifyTrackId);
            }
            


        }

        void PlayPlayList(string playListId)
        {
            if (Command1Package.spotWeb == null)
            {
                MessageBox.Show("Cant retrieve Play list, not authenticated.");
                return;
            }
            Paging<PlaylistTrack> listTracks = Command1Package.spotWeb.GetPlaylistTracks(userId, playListId, "", kMAX_TRACKS);
            List<string> songURIs = new List<string>();


            if (listTracks.Total > 0)
            {
                for (int i = 0; i < listTracks.Items.Count; i++)
                {
                    if (listTracks.Items[i].Track.IsPlayable ?? true)
                    {
                        string trackUri = listTracks.Items[i].Track.Uri;
                        songURIs.Add(trackUri);
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new System.Action(() => MessageBox.Show("Track not available: " + listTracks.Items[i].Track.Name)));
                    }
                }

                Command1Package.spotWeb.ResumePlayback("", "", songURIs);
            }
            
        }
        void PlayPlayListUri(string playListUri)
        {

            Command1Package.spotClient.PlayURL(playListUri);
        }


        private void Sa_OnResponseReceivedEvent(SpotifyAPI.Web.Auth.AutorizationCodeAuthResponse response)
        {
            Dispatcher.BeginInvoke(new System.Action(() => MessageBox.Show("Response: " + response.ToString())));
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIndex = listView.SelectedIndex;
            if(listView.Items.Count == 0)
            {
                return;
            }

            Label l = ((PlayList.SpotListViewItem)e.AddedItems[0]).Name as Label;
            l.Foreground = new SolidColorBrush(foregroundColor);
            if(e.RemovedItems.Count > 0)
            {
                Label lr = ((PlayList.SpotListViewItem)e.RemovedItems[0]).Name as Label;
                lr.Foreground = foregroundLabelBrush;
            }
            
        }

        private void listView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //MessageBox.Show("Clicked: ");
            if (lvm == ListViewMode.kPlayList)
            {
#if LOCAL_PLAY
                PlayPlayListUri(playListUri[selectedIndex]);
#else
             PlayPlayList(playListId[selectedIndex]);
#endif
            }
            else
            {
                PlayPlayListUri(listTracksFromPL[selectedIndex].Track.Uri);
            }
            



        }

        private void PlayListWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecalculateListView();
        }

        private void RecalculateListView()
        {
            if (lvm == ListViewMode.kPlayList)
            {
                listView.Height = this.ActualHeight - WindowTitle.ActualHeight;
            }
            else
            {
                listView.Height = (this.ActualHeight - WindowTitle.ActualHeight) - ReturnPlaylists.ActualHeight;
            }
        }

        private void ReturnPlaylists_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            listView.Items.Clear();
            ShowPlayListsInView();
        }
    }
}