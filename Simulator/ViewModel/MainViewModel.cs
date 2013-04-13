using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private int _memoryPageOffset;
		#endregion

		#region Public Properties
		/// <summary>
		/// The Processor
		/// </summary>
		public Proc Proc { get; set; }

		public ObservableCollection<MemoryRowModel> MemoryPage { get; set; }
		
		public string MemoryPageOffset
		{
			get { return _memoryPageOffset.ToString("X"); }
			set
			{
				//I don't like this hack, but WPF doesn't support any way to update the Value on keypress
				if (string.IsNullOrEmpty(value))
					return;
				try
				{
					_memoryPageOffset = Convert.ToInt32(value, 16);
				}
// ReSharper disable EmptyGeneralCatchClause
				catch {}
// ReSharper restore EmptyGeneralCatchClause
				
			}
		}

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

		/// <summary>
		/// Relay Command that updates the Memory Map when the Page changes
		/// </summary>
		public RelayCommand UpdateMemoryMapCommand { get; set; }
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
			UpdateMemoryMapCommand = new RelayCommand(UpdateMemoryPage);

			Messenger.Default.Register<NotificationMessage<OpenFileModel>>(this, FileOpenedNotification);
			FilePath = "No File Loaded";

			MemoryPage = new ObservableCollection<MemoryRowModel>();
			UpdateMemoryPage();
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

			UpdateMemoryPage();
			RaisePropertyChanged("MemoryPage");
		}

		private void UpdateMemoryPage()
		{
			MemoryPage.Clear();
			var offset = ( _memoryPageOffset * 256 );

			var multiplyer = 0;
			for (var i = offset; i < 256 * (_memoryPageOffset + 1); i++)
			{
				
				MemoryPage.Add(new MemoryRowModel
					               {
						               Offset = ((16 * multiplyer) + offset).ToString("X").PadLeft(4, '0'),
									   Location00 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2,'0'),
									   Location01 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location02 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location03 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location04 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location05 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location06 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location07 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location08 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location09 = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location0A = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location0B = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location0C = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location0D = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location0E = Proc.Memory.ReadValue(i++).ToString("X").PadLeft(2, '0'),
									   Location0F = Proc.Memory.ReadValue(i).ToString("X").PadLeft(2, '0'),
									});
				multiplyer++;
			}
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