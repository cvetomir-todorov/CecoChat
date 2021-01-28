using System.Collections.Specialized;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public sealed class ScrollIntoViewBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            INotifyCollectionChanged items = AssociatedObject.Items;
            items.CollectionChanged += ListViewOnCollectionChanged;
        }

        protected override void OnDetaching()
        {
            INotifyCollectionChanged items = AssociatedObject.Items;
            items.CollectionChanged -= ListViewOnCollectionChanged;
        }

        private void ListViewOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                AssociatedObject.ScrollIntoView(e.NewItems[^1]);
            }
        }
    }
}
