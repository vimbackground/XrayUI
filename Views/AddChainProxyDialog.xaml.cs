using System.Collections.Generic;
using System.Linq;
using XrayUI.Models;

namespace XrayUI.Views
{
    public sealed partial class AddChainProxyDialog
    {
        private readonly List<ServerEntry> _servers;
        private readonly ServerEntry? _existing;

        public AddChainProxyDialog(IEnumerable<ServerEntry>? servers = null, ServerEntry? existing = null)
        {
            this.InitializeComponent();
            _existing = existing;
            _servers = servers?
                .Where(server => !server.IsChain)
                .ToList() ?? [];

            EntryComboBox.ItemsSource = _servers;
            ExitComboBox.ItemsSource = _servers;

            if (existing is not null)
            {
                NameTextBox.Text = existing.Name;
                EntryComboBox.SelectedItem = _servers.FirstOrDefault(
                    server => server.Id == existing.ChainEntryServerId);
                ExitComboBox.SelectedItem = _servers.FirstOrDefault(
                    server => server.Id == existing.ChainExitServerId);
            }
        }

        public bool TryCreateOrUpdate(out ServerEntry? entry)
        {
            entry = null;
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;

            var name = NameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("请输入链式代理名称。");
                return false;
            }

            if (EntryComboBox.SelectedItem is not ServerEntry entryServer)
            {
                ShowError("请选择入口代理。");
                return false;
            }

            if (ExitComboBox.SelectedItem is not ServerEntry exitServer)
            {
                ShowError("请选择出口代理。");
                return false;
            }

            if (entryServer.Id == exitServer.Id)
            {
                ShowError("入口代理和出口代理不能是同一个节点。");
                return false;
            }

            var chain = _existing ?? new ServerEntry();
            chain.Name = name;
            chain.SubscriptionId = string.Empty;
            chain.Protocol = "chain";
            // Host/Port mirror the entry server so latency probing has an endpoint.
            chain.Host = entryServer.Host;
            chain.Port = entryServer.Port;
            chain.ChainEntryServerId = entryServer.Id;
            chain.ChainExitServerId = exitServer.Id;

            entry = chain;
            return true;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
