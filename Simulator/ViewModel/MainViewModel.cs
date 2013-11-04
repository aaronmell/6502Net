using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
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
		private readonly BackgroundWorker _backgroundWorker;
		
		#endregion

		#region Public Properties
		/// <summary>
		/// The Processor
		/// </summary>
		public Proc Proc { get; set; }

		/// <summary>
		/// The Current Memory Page
		/// </summary>
		public MultiThreadedObservableCollection<MemoryRowModel> MemoryPage { get; set; }
		
		/// <summary>
		/// The Current Stack
		/// </summary>
		public MultiThreadedObservableCollection<MemoryRowModel> Stack { get; set; } 
		
		/// <summary>
		/// The output log
		/// </summary>
		public MultiThreadedObservableCollection<OutputLog> OutputLog { get; private set; }
		
		/// <summary>
		/// The Current Disassembly
		/// </summary>
		public string CurrentDisassembly
		{
			get { return string.Format("{0} {1}", Proc.CurrentDisassembly.OpCodeString, Proc.CurrentDisassembly.DisassemblyOutput); }
		}

		/// <summary>
		/// The number of cycles.
		/// </summary>
		public int NumberOfCycles { get; private set; }

		/// <summary>
		/// The Memory Page Offset. IE: Which page are we looking at
		/// </summary>
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
		/// The Assembler Listing
		/// </summary>
		public string Listing { get; set; }

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
		/// The Slider CPU Speed
		/// </summary>
		public int CpuSpeed { get; set; }

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

		public RelayCommand SaveStateCommand { get; set; }
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
			SaveStateCommand = new RelayCommand(SaveState);

			Messenger.Default.Register<NotificationMessage<OpenFileModel>>(this, FileOpenedNotification);
			FilePath = "No File Loaded";

			MemoryPage = new MultiThreadedObservableCollection<MemoryRowModel>();
			Stack = new MultiThreadedObservableCollection<MemoryRowModel>();
			OutputLog = new MultiThreadedObservableCollection<OutputLog>();

			UpdateMemoryPage();
			UpdateStack();

			_backgroundWorker = new BackgroundWorker {WorkerSupportsCancellation = true, WorkerReportsProgress = false};
			_backgroundWorker.DoWork += BackgroundWorkerDoWork;
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

			Listing = notificationMessage.Content.Listing;
			RaisePropertyChanged("Listing");

			Reset();
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

		private void UpdateStack()
		{
			Stack.Clear();

			for (var i = 0x1FF; i > 0x100; i--)
			{

				Stack.Add(new MemoryRowModel
				{
					Offset = (i - 15).ToString("X").PadLeft(4, '0'),
					Location0F = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location0E = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location0D = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location0C = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location0B = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location0A = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location09 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location08 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location07 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location06 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location05 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location04 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location03 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location02 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location01 = Proc.Memory.ReadValue(i--).ToString("X").PadLeft(2, '0'),
					Location00 = Proc.Memory.ReadValue(i).ToString("X").PadLeft(2, '0'),
				});
			}
		}

		private void Reset()
		{
			IsRunning = false;

			if (_backgroundWorker.IsBusy)
				_backgroundWorker.CancelAsync();

			Proc.Reset();
			RaisePropertyChanged("Proc");
			
			IsRunning = false;
			NumberOfCycles = 0;
			RaisePropertyChanged("NumberOfCycles");
			
			UpdateMemoryPage();
			RaisePropertyChanged("MemoryPage");

			UpdateStack();
			RaisePropertyChanged("Stack");

			OutputLog.Clear();
			RaisePropertyChanged("CurrentDisassembly");

			OutputLog.Insert(0, GetOutputLog());
			UpdateUi();
		}

		private void Step()
		{
			IsRunning = false;

			if (_backgroundWorker.IsBusy)
				_backgroundWorker.CancelAsync();
				
			StepProcessor();
			UpdateMemoryPage();
			UpdateStack();

			OutputLog.Insert(0, GetOutputLog());
			UpdateUi();
		}

		private void UpdateUi()
		{
			RaisePropertyChanged("Proc");
			RaisePropertyChanged("NumberOfCycles");
			RaisePropertyChanged("CurrentDisassembly");
			RaisePropertyChanged("MemoryPage");
			RaisePropertyChanged("Stack");
		}

		private void StepProcessor()
		{
			NumberOfCycles++;
			Proc.NextStep();
		}

		private OutputLog GetOutputLog()
		{
			return new OutputLog(Proc.CurrentDisassembly)
				                    {
					                    XRegister = Proc.XRegister.ToString("X").PadLeft(2,'0'),
										YRegister = Proc.YRegister.ToString("X").PadLeft(2,'0'),
										Accumulator =  Proc.Accumulator.ToString("X").PadLeft(2,'0'),
										NumberOfCycles = NumberOfCycles,
										StackPointer = Proc.StackPointer.ToString("X").PadLeft(2, '0'),
										ProgramCounter = Proc.ProgramCounter.ToString("X").PadLeft(4, '0'),
										CurrentOpCode = Proc.CurrentOpCode.ToString("X").PadLeft(2, '0')
				                    };
		}

		private void RunPause()
		{
			var isRunning = !IsRunning;

			if (isRunning)
				_backgroundWorker.RunWorkerAsync();
			else
				_backgroundWorker.CancelAsync();

			IsRunning = !IsRunning;
		}

		private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
		{
			var worker = sender as BackgroundWorker;
			var outputLogs = new List<OutputLog>();
			
			while (true)
			{
				if (worker != null && worker.CancellationPending)
				{
					e.Cancel = true;

					RaisePropertyChanged("Proc");
					
					foreach (var log in outputLogs)
						OutputLog.Insert(0,log);

					UpdateMemoryPage();
					UpdateStack();
					return;
				}

				StepProcessor();
				outputLogs.Add(GetOutputLog());

				if (NumberOfCycles % GetLogModValue() == 0)
				{
					foreach (var log in outputLogs)
						OutputLog.Insert(0, log);

					outputLogs.Clear();
					UpdateUi();
				}
				Thread.Sleep(GetSleepValue());
			}
		}

		private int GetLogModValue()
		{
			switch (CpuSpeed)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
					return 1;
				case 6:
					return 5;
				case 7:
					return 20;
				case 8:
					return 30;
				case 9:
					return 40;
				case 10:
					return 50;
				default:
					return 5;
			}
		}

		private int GetSleepValue()
		{
			switch (CpuSpeed)
			{
				case 0:
					return 550;
				case 1:
					return 550;
				case 2:
					return 440;
				case 3:
					return 330;
				case 4:
					return 220;
				case 5:
					return 160;
				case 6:
					return 80;
				case 7:
					return 40;
				case 8:
					return 20;
				case 9:
					return 10;
				case 10:
					return 5;
				default:
					return 5;
			}
		}

		private void OpenFile()
		{
			IsRunning = false;

			if (_backgroundWorker.IsBusy)
				_backgroundWorker.CancelAsync();

			Messenger.Default.Send(new NotificationMessage("OpenFileWindow"));
		}

		private void SaveState()
		{
			IsRunning = false;

			if (_backgroundWorker.IsBusy)
				_backgroundWorker.CancelAsync();

			//Messenger.Default.Send(new NotificationMessage("SaveFileWindow"));

			Messenger.Default.Send(new NotificationMessage<SaveFileModel>(new SaveFileModel
				{
					NumberOfCycles = NumberOfCycles,
					Listing = Listing,

					ProgramCounter = Proc.ProgramCounter,
					MemoryDump = Proc.Memory.DumpMemory(),
					StackPointer = Proc.StackPointer,
					Accumulator = Proc.Accumulator,
					XRegister = Proc.XRegister,
					YRegister = Proc.YRegister,
					CurrentOpCode = Proc.CurrentOpCode,
					CurrentDisassembly = Proc.CurrentDisassembly,
					InterruptPeriod = Proc.InterruptPeriod,
					DisableInterruptFlag = Proc.DisableInterruptFlag,
					NumberofCyclesLeft = Proc.NumberofCyclesLeft,
					CarryFlag = Proc.CarryFlag,
					ZeroFlag = Proc.ZeroFlag,
					DecimalFlag = Proc.DecimalFlag,
					OverflowFlag = Proc.OverflowFlag,
					NegativeFlag = Proc.NegativeFlag
				}, "SaveFileWindow"));
		}
		#endregion
	}
}