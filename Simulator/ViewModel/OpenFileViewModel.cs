using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Simulator.Model;

namespace Simulator.ViewModel
{
	public class OpenFileViewModel : ViewModelBase
	{
		public RelayCommand LoadProgramCommand { get; set; }

		public RelayCommand CloseCommand { get; set; }

		public RelayCommand SelectFileCommand { get; set; }

		public string Filename { get; set; }

		public string InitalProgramCounter { get; set; }

		public string MemoryOffset { get; set; }

		public bool LoadEnabled { get { return !string.IsNullOrEmpty(Filename); }}

		public bool IsNotStateFile
		{
			get
			{
				if (string.IsNullOrEmpty(Filename))
					return true;

				return !Filename.EndsWith(".6502");
			}
		}

		public OpenFileViewModel()
		{
			LoadProgramCommand = new RelayCommand(Load);
			CloseCommand = new RelayCommand(Close);
			SelectFileCommand = new RelayCommand(Select);

			InitalProgramCounter = "0x0000";
			MemoryOffset = "0x0000";
		}

		private void Load()
		{
			var extension = Path.GetExtension(Filename);
			if (extension != null && extension.ToUpper() == ".BIN" && !TryLoadBinFile())
				return;

			if (extension != null && extension.ToUpper() == ".6502" && !TryLoad6502File())
					return;

			Close();
		}

		private bool TryLoad6502File()
		{
			var formatter = new BinaryFormatter();
			Stream stream = new FileStream(Filename, FileMode.Open);
			
			var fileModel = (StateFileModel)formatter.Deserialize(stream);
			fileModel.FilePath = Filename;

			stream.Close();
			
			Messenger.Default.Send(new NotificationMessage<StateFileModel>(fileModel, "FileLoaded"));

			return true;
		}

		private bool TryLoadBinFile()
		{
			int programCounter;
			try
			{
				programCounter = Convert.ToInt32(InitalProgramCounter, 16);
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Parse ProgramCounter into int");
				return false;
			}

			int memoryOffset;
			try
			{
				memoryOffset = Convert.ToInt32(MemoryOffset, 16);
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Parse Memory Offset into int");
				return false;
			}

			byte[] program;
			try
			{
				program = File.ReadAllBytes(Filename);
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Open Program Binary");
				return false;
			}

			string listing;
			try
			{
				// ReSharper disable AssignNullToNotNullAttribute
				listing = File.ReadAllText(string.Format("{0}.lst", Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename))));
				// ReSharper restore AssignNullToNotNullAttribute
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Open Program Listing");
				return false;
			}

			Messenger.Default.Send(new NotificationMessage<AssemblyFileModel>(new AssemblyFileModel
			{
				InitialProgramCounter = programCounter,
				MemoryOffset = memoryOffset,
				Listing = listing,
				Program = program,
				FilePath = Filename
			}, "FileLoaded"));

			return true;
		}
		
		private static void Close()
		{
			Messenger.Default.Send(new NotificationMessage("CloseFileWindow"));
		}

		private void Select()
		{
			var dialog = new OpenFileDialog {DefaultExt = ".bin", Filter = "All Files (*.bin, *.6502)|*.bin;*.6502|Binary Assembly (*.bin)|*.bin|6502 Simulator Save State (*.6502)|*.6502"};

			var result = dialog.ShowDialog();

			if (result != true)
				return;

			Filename = dialog.FileName;
			RaisePropertyChanged("Filename");
			RaisePropertyChanged("LoadEnabled");
			RaisePropertyChanged("IsNotStateFile");
		}
	}
}
