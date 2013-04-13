using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Simulator.Model;
using Proc= Processor.Processor;

namespace Simulator.ViewModel
{
	/// <summary>
	/// The Main ViewModel
	/// </summary>
	public class MainViewModel : ViewModelBase
	{
		private bool _isRunning;
		private bool _isPaused;
		

		public Proc Proc { get; set; }

		public bool IsRunning
		{
			get { return _isRunning; }
			set
			{
				_isRunning = value;
				RaisePropertyChanged("IsRunning");
			}
		}

		public bool IsPaused
		{
			get { return _isPaused; }
			set 
			{ 
				_isPaused = value;
				RaisePropertyChanged("IsRunning"); 
			}
		}

		public RelayCommand StepCommand { get; set; }

		public RelayCommand StartCommand { get; set; }

		public RelayCommand ResetCommand { get; set; }

		public RelayCommand RunCommand { get; set; }

		public RelayCommand OpenCommand { get; set; }

		public RelayCommand PauseCommand { get; set; }

		public MainViewModel()
		{
			Proc = new Proc();
			Proc.Reset();

			ResetCommand = new RelayCommand(Reset);
			StepCommand = new RelayCommand(Step);
			StartCommand = new RelayCommand(Start);
			OpenCommand = new RelayCommand(OpenFile);
			PauseCommand = new RelayCommand(Pause);
			Messenger.Default.Register<NotificationMessage<OpenFileModel>>(this, FileOpenedNotification);
		}

		private void FileOpenedNotification(NotificationMessage<OpenFileModel> notificationMessage)
		{
			if (notificationMessage.Notification != "FileLoaded")
			{
				return;
			}

			Proc.LoadProgram(notificationMessage.Content.MemoryOffset, notificationMessage.Content.Program, notificationMessage.Content.InitialProgramCounter);
			MessageBox.Show("Program Loaded");
		}

		private void Reset()
		{
			Proc.Reset();
			RaisePropertyChanged("Proc");
			IsRunning = false;
			IsPaused = true;
		}

		private void Step()
		{
			IsRunning = true;
			IsPaused = true;

			Proc.NextStep();
			RaisePropertyChanged("Proc");
		}

		private void Start()
		{
			IsRunning = !IsRunning;
		}

		private void Pause()
		{
			IsPaused = !IsPaused;
		}

		public void OpenFile()
		{
			Messenger.Default.Send(new NotificationMessage("OpenFileWindow"));
		}
	}
}