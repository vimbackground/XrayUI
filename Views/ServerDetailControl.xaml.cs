using System;
using Windows.System;

namespace XrayUI.Views
{
    public sealed partial class ServerDetailControl
    {
        public ServerDetailViewModel ViewModel { get; set; } = null!;

        // The three AI service Borders share this handler. Without a guard, each Border's
        // Loaded fire would re-add AIShadowCastGrid to all three Shadows — three Borders ×
        // three receivers = nine entries, with duplicates causing wasted compositor work.
        private bool _shadowsWired;

        public ServerDetailControl()
        {
            this.InitializeComponent();
        }

        private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
        {
            if (_shadowsWired) return;
            _shadowsWired = true;
            Shadow1.Receivers.Add(AIShadowCastGrid);
            Shadow2.Receivers.Add(AIShadowCastGrid);
            Shadow3.Receivers.Add(AIShadowCastGrid);
        }

        private async void AiLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string url } || string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return;

            await Launcher.LaunchUriAsync(uri);
        }
    }
}
