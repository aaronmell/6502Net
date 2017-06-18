using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
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
		#region Fields
		private bool _isRunning;
		private int _memoryPageOffset;
		private readonly BackgroundWorker _backgroundWorker;
		private bool _breakpointTriggered;
		#endregion

		#region Properties
		/// <summary>
		/// The Processor
		/// </summary>
		public Proc Proc { get; set; }

		/// <summary>
		/// The Current Memory Page
		/// </summary>
		public MultiThreadedObservableCollection<MemoryRowModel> MemoryPage { get; set; }
		
		/// <summary>
		/// The output log
		/// </summary>
		public MultiThreadedObservableCollection<OutputLog> OutputLog { get; private set; }
		
		/// <summary>
		/// The Breakpoints
		/// </summary>
		public MultiThreadedObservableCollection<Breakpoint> Breakpoints { get; set; } 

		/// <summary>
		/// The Currently Selected Breakpoint
		/// </summary>
		public Breakpoint SelectedBreakpoint { get; set; }

		/// <summary>
		/// The Current Disassembly
		/// </summary>
		public string CurrentDisassembly
		{
			get 
            { 
                if (Proc.CurrentDisassembly != null) 
                    return string.Format("{0} {1}", Proc.CurrentDisassembly.OpCodeString, Proc.CurrentDisassembly.DisassemblyOutput);

			    else
                {
                    return string.Empty;
                }
            }
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

		/// <summary>
		/// Relay Command the Saves the Current State to File
		/// </summary>
		public RelayCommand SaveStateCommand { get; set; }

		/// <summary>
		/// The Relay Command that adds a new breakpoint
		/// </summary>
		public RelayCommand AddBreakPointCommand { get; set; }

		/// <summary>
		/// The Relay Command that Removes an existing breakpoint
		/// </summary>
		public RelayCommand RemoveBreakPointCommand { get; set; }
		
		/// <summary>
		/// The Command that sends an IRQ or Interrupt Request to the processor
		/// </summary>
		public RelayCommand SendInterruptRequestCommand { get; set; }

		/// <summary>
		/// The Command that sends an NMI or non-maskable Interrupt to the processor
		/// </summary>
		public RelayCommand SendNonMaskableInterruptComand { get; set; }
		#endregion

		#region public Methods
		/// <summary>
		/// Creates a new Instance of the MainViewModel
		/// </summary>
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
			AddBreakPointCommand = new RelayCommand(AddBreakPoint);
			RemoveBreakPointCommand = new RelayCommand(RemoveBreakPoint);
			SendNonMaskableInterruptComand = new RelayCommand(SendNonMaskableInterrupt);
			SendInterruptRequestCommand = new RelayCommand(SendInterruptRequest);


			Messenger.Default.Register<NotificationMessage<AssemblyFileModel>>(this, FileOpenedNotification);
			Messenger.Default.Register<NotificationMessage<StateFileModel>>(this, StateLoadedNotifcation);
			FilePath = "No File Loaded";

			MemoryPage = new MultiThreadedObservableCollection<MemoryRowModel>();
			OutputLog = new MultiThreadedObservableCollection<OutputLog>();
			Breakpoints = new MultiThreadedObservableCollection<Breakpoint>();
			
			UpdateMemoryPage();

			_backgroundWorker = new BackgroundWorker {WorkerSupportsCancellation = true, WorkerReportsProgress = false};
			_backgroundWorker.DoWork += BackgroundWorkerDoWork;
		}
		#endregion

		#region Private Methods
		private void FileOpenedNotification(NotificationMessage<AssemblyFileModel> notificationMessage)
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

		private void StateLoadedNotifcation(NotificationMessage<StateFileModel> notificationMessage)
		{
			if (notificationMessage.Notification != "FileLoaded")
			{
				return;
			}

			Reset();

			FilePath = string.Format("Loaded State: {0}", notificationMessage.Content.FilePath);
			RaisePropertyChanged("FilePath");

			Listing = notificationMessage.Content.Listing;
			RaisePropertyChanged("Listing");

			OutputLog = new MultiThreadedObservableCollection<OutputLog>(notificationMessage.Content.OutputLog);
			RaisePropertyChanged("OutputLog");

			NumberOfCycles = notificationMessage.Content.NumberOfCycles;

			Proc = notificationMessage.Content.Processor;
			UpdateMemoryPage();
			UpdateUi();

			IsProgramLoaded = true;
			RaisePropertyChanged("IsProgramLoaded");
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
									   Location00 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2,'0'),
									   Location01 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location02 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location03 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location04 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location05 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location06 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location07 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location08 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location09 = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location0A = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location0B = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location0C = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location0D = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location0E = Proc.ReadMemoryValueWithoutCycle(i++).ToString("X").PadLeft(2, '0'),
									   Location0F = Proc.ReadMemoryValueWithoutCycle(i).ToString("X").PadLeft(2, '0'),
									});
				multiplyer++;
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

			OutputLog.Insert(0, GetOutputLog());
			UpdateUi();
		}

		private void UpdateUi()
		{
			RaisePropertyChanged("Proc");
			RaisePropertyChanged("NumberOfCycles");
			RaisePropertyChanged("CurrentDisassembly");
			RaisePropertyChanged("MemoryPage");
		}

		private void StepProcessor()
		{
			Proc.NextStep();
            NumberOfCycles = Proc.GetCycleCount();
        }

		private OutputLog GetOutputLog()
		{
            if (Proc.CurrentDisassembly == null)
            {
                return new OutputLog(new Processor.Disassembly());
            }

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
				if (worker != null && worker.CancellationPending || IsBreakPointTriggered())
				{
					e.Cancel = true;

					RaisePropertyChanged("Proc");
					
					foreach (var log in outputLogs)
						OutputLog.Insert(0,log);

					UpdateMemoryPage();
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

		private bool IsBreakPointTriggered()
		{
			//This prevents the Run Command from getting stuck after reaching a breakpoint
			if (_breakpointTriggered)
			{
				_breakpointTriggered = false;
				return false;
			}

			foreach (var breakpoint in Breakpoints.Where(x => x.IsEnabled))
			{
				int value ;

				if (!int.TryParse(breakpoint.Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
					continue;

				if (breakpoint.Type == BreakpointType.NumberOfCycleType && value == NumberOfCycles)
				{
					_breakpointTriggered = true;
					RunPause();
					return true;
				}

				if (breakpoint.Type == BreakpointType.ProgramCounterType && value == Proc.ProgramCounter)
				{
					_breakpointTriggered = true;
					RunPause();
					return true;
				}
			}

			return false;
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

			Messenger.Default.Send(new NotificationMessage<StateFileModel>(new StateFileModel
				{
					NumberOfCycles = NumberOfCycles,
					Listing = Listing,
					OutputLog = OutputLog.ToList(),
					Processor = Proc
				}, "SaveFileWindow"));
		}

		private void AddBreakPoint()
		{
			Breakpoints.Add(new Breakpoint());
			RaisePropertyChanged("Breakpoints");
		}

		private void RemoveBreakPoint()
		{
			if (SelectedBreakpoint == null)
				return;

			Breakpoints.Remove(SelectedBreakpoint);
			SelectedBreakpoint = null;
			RaisePropertyChanged("SelectedBreakpoint");

		}

		private void SendNonMaskableInterrupt()
		{
			IsRunning = false;

			if (_backgroundWorker.IsBusy)
				_backgroundWorker.CancelAsync();

			Proc.NonMaskableInterrupt();

			UpdateMemoryPage();

			OutputLog.Insert(0, GetOutputLog());
			UpdateUi();
		}

		private void SendInterruptRequest()
		{
			IsRunning = false;

			if (_backgroundWorker.IsBusy)
				_backgroundWorker.CancelAsync();

			Proc.InterruptRequest();
			
			UpdateMemoryPage();

			OutputLog.Insert(0, GetOutputLog());
			UpdateUi();
		}
		#endregion
	}
}