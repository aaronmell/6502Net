using GalaSoft.MvvmLight.Messaging;

namespace Simulator
{
	/// <summary>
	/// Interaction logic for OpenFile.xaml
	/// </summary>
	public partial class OpenFile
	{
		public OpenFile()
		{
			InitializeComponent();
			Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
		}

		private void NotificationMessageReceived(NotificationMessage notificationMessage)
		{
			if (notificationMessage.Notification == "CloseFileWindow")
				Close();
		}
	}
}
