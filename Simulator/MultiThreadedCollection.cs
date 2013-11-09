using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace Simulator
{
	/// <summary>
	/// A MultiThreaedObservableCollection. 
	/// This allows multiple threads to access the same observable collection in a safe manner.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class MultiThreadedObservableCollection<T> : ObservableCollection<T>
	{
		/// <summary>
		/// Instantiates a new instance of the MultiThreadedObservableCollection
		/// </summary>
		public MultiThreadedObservableCollection()
		{
			
		}

		/// <summary>
		///  Instantiates a new instance of the MultiThreadedObservableCollection
		/// </summary>
		/// <param name="collection">The initial collection to be loaded</param>
		public MultiThreadedObservableCollection(IEnumerable<T> collection)
			: base(collection)
		{
			
		}

		/// <summary>
		///  Instantiates a new instance of the MultiThreadedObservableCollection
		/// </summary>
		/// <param name="list">The initial list to be loaded</param>
		public MultiThreadedObservableCollection(List<T> list)
			: base(list)
		{

		} 

		/// <summary>
		/// The NotifyCollectionChangedEventHandler, Sends a notification anytime the collection has been modified.
		/// </summary>
		public override event NotifyCollectionChangedEventHandler CollectionChanged;


		/// <summary>
		/// The NotifyCollectionChangedEventHandler, Notifies the listeners in a thread safe manner
		/// </summary>
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
