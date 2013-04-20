using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace Simulator
{
	public class MultiThreadedObservableCollection<T> : ObservableCollection<T>
	{
		public override event NotifyCollectionChangedEventHandler CollectionChanged;
		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			var collectionChanged = CollectionChanged;
			if (collectionChanged != null)
				foreach (NotifyCollectionChangedEventHandler nh in collectionChanged.GetInvocationList())
				{
					var dispObj = nh.Target as DispatcherObject;
					if (dispObj != null)
					{
						var dispatcher = dispObj.Dispatcher;
						if (dispatcher != null && !dispatcher.CheckAccess())
						{
							var nh1 = nh;
							dispatcher.BeginInvoke(
								(Action)(() => nh1.Invoke(this,
									new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
								DispatcherPriority.DataBind);
							continue;
						}
					}
					nh.Invoke(this, e);
				}
		}
	}
}
