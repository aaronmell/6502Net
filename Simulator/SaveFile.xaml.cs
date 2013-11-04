using GalaSoft.MvvmLight.Messaging;

namespace Simulator
{
	/// <summary>
	/// Interaction logic for SaveState.xaml
	/// </summary>
	public partial class SaveFile
	{
		public SaveFile()
		{
			InitializeComponent();
			Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
		}

		private void NotificationMessageReceived(NotificationMessage notificationMessage)
		{
			if (notificationMessage.Notification == "CloseSaveFileWindow")
				Close();
		}
	}
}
