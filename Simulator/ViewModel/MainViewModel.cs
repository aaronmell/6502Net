using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Simulator.Model;
using Proc = Processor.Processor;

namespace Simulator.ViewModel
{
	/// <summary>
	/// The Main ViewModel
	/// </summary>
	public class MainViewModel : ViewModelBase
	{
		#region Private Properties
		private bool _isRunning;
		#endregion

		#region Public Properties
		/// <summary>
		/// The Processor
		/// </summary>
		public Proc Proc { get; set; }

		/// <summary>
		///  Is the Prorgam Running
		/// </summary>
		public bool IsRunning
		{
			get { return _isRunning; }
			set
			{
				_isRunning = value;
				RaisePropertyChanged("IsRunning");
			}
		}

		/// <summary>
		/// Is a Program Loaded.
		/// </summary>
		public bool IsProgramLoaded { get; set; }

		/// <summary>
		/// The Path to the Program that is running
		/// </summary>
		public string FilePath { get; private set; }

		/// <summary>
		/// RelayCommand for Stepping through the progam one instruction at a time.
		/// </summary>
		public RelayCommand StepCommand { get; set; }

		/// <summary>
		/// Relay Command to Reset the Program back to its initial state.
		/// </summary>
		public RelayCommand ResetCommand { get; set; }

		/// <summary>
		/// Relay Command that Run/Pauses Execution
		/// </summary>
		public RelayCommand RunPauseCommand { get; set; }

		/// <summary>
		/// Relay Command that opens a new file
		/// </summary>
		public RelayCommand OpenCommand { get; set; }
		#endregion

		#region public Methods
		public MainViewModel()
		{
			Proc = new Proc();
			Proc.Reset();

			ResetCommand = new RelayCommand(Reset);
			StepCommand = new RelayCommand(Step);
			OpenCommand = new RelayCommand(OpenFile);
			RunPauseCommand = new RelayCommand(RunPause);

			Messenger.Default.Register<NotificationMessage<OpenFileModel>>(this, FileOpenedNotification);
			FilePath = "No File Loaded";
		}

		#endregion

		#region Private Methods
		private void FileOpenedNotification(NotificationMessage<OpenFileModel> notificationMessage)
		{
			if (notificationMessage.Notification != "FileLoaded")
			{
				return;
			}

			Proc.LoadProgram(notificationMessage.Content.MemoryOffset, notificationMessage.Content.Program, notificationMessage.Content.InitialProgramCounter);
			FilePath = string.Format("Loaded Program: {0}", notificationMessage.Content.FilePath);
			RaisePropertyChanged("FilePath");

			IsProgramLoaded = true;
			RaisePropertyChanged("IsProgramLoaded");
		}

		private void Reset()
		{
			Proc.Reset();
			RaisePropertyChanged("Proc");
			IsRunning = false;
		}

		private void Step()
		{
			IsRunning = false;

			Proc.NextStep();
			RaisePropertyChanged("Proc");
		}

		private void RunPause()
		{
			IsRunning = !IsRunning;
		}

		private static void OpenFile()
		{
			Messenger.Default.Send(new NotificationMessage("OpenFileWindow"));
		}
		#endregion
	}
}