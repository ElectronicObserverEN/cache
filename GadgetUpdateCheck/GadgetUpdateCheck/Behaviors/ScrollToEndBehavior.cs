using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace GadgetUpdateCheck.Behaviors;

public class ScrollToEndBehavior : Behavior<ListBox>
{
	private INotifyCollectionChanged? _collection;

	protected override void OnAttached()
	{
		base.OnAttached();
		UpdateCollection();
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		if (_collection != null)
		{
			_collection.CollectionChanged -= Collection_CollectionChanged;
		}
	}

	private void UpdateCollection()
	{
		_collection = AssociatedObject?.Items;

		if (_collection != null)
		{
			_collection.CollectionChanged += Collection_CollectionChanged;
		}
	}

	private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action is NotifyCollectionChangedAction.Add && e.NewItems?[^1] is { } item)
		{
			AssociatedObject?.ScrollIntoView(item);
		}
	}
}