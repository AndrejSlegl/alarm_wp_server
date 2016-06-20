using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace AlarmServer
{
    public class AudioPlayer : IAudioPlayer
    {
        readonly Popup popup = new Popup();
        readonly MediaElement mediaElement = new MediaElement();

        public Uri SourceUri
        {
            get { return mediaElement.Source; }
        }

        public AudioPlayer()
        {
            mediaElement.AutoPlay = true;
            popup.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            popup.Child = mediaElement;
            popup.IsOpen = true;
        }

        public void Play(Uri sourceUri, bool isLooping)
        {
            mediaElement.IsLooping = isLooping;
            mediaElement.Source = sourceUri;
        }

        public void Stop()
        {
            mediaElement.Source = null;
        }

        public void Dispose()
        {
            popup.IsOpen = false;
            mediaElement.Source = null;
        }
    }
}
