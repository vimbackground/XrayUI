using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using XrayUI.Helpers;
using XrayUI.Models;

namespace XrayUI.Views
{
    public sealed partial class ManageSubscriptionsDialog : UserControl
    {
        public ManageSubscriptionsViewModel ViewModel { get; }

        public ManageSubscriptionsDialog(ManageSubscriptionsViewModel vm)
        {
            ViewModel = vm;
            InitializeComponent();

            ToolTipService.SetToolTip(AddPageSegment,    L.Subscription_AddTooltip);
            ToolTipService.SetToolTip(ManagePageSegment, L.Subscription_ManageTooltip);
        }

        private void RefreshButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                ToolTipService.SetToolTip(element, L.Subscription_Refresh);
        }

        private void DeleteButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                ToolTipService.SetToolTip(element, L.Subscription_DeleteTooltip);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: SubscriptionEntry sub })
                ViewModel.RefreshSubscriptionCommand.Execute(sub);
        }

        private void ConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: SubscriptionEntry sub } btn)
            {
                HideAncestorFlyout(btn);
                ViewModel.DeleteSubscriptionCommand.Execute(sub);
            }
        }

        private static void HideAncestorFlyout(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is FlyoutPresenter fp && fp.Parent is Popup popup)
                {
                    popup.IsOpen = false;
                    return;
                }
                current = VisualTreeHelper.GetParent(current);
            }
        }
    }
}
