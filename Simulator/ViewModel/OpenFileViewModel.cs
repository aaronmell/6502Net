using System;
using System.IO;
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
		//public OpenFileModel OpenFileModel { get; set; }

		public RelayCommand LoadProgramCommand { get; set; }

		public RelayCommand CloseCommand { get; set; }

		public RelayCommand SelectFileCommand { get; set; }

		public string Filename { get; set; }

		public string InitalProgramCounter { get; set; }

		public string MemoryOffset { get; set; }

		public bool LoadEnabled { get { return !string.IsNullOrEmpty(Filename); }}
		

		public OpenFileViewModel()
		{
			LoadProgramCommand = new RelayCommand(Load);
			CloseCommand = new RelayCommand(Close);
			SelectFileCommand = new RelayCommand(Select);

			InitalProgramCounter = "0x0000";
			MemoryOffset = "0x000";
		}

		private void Load()
		{
			int programCounter;
			try
			{
				programCounter = Convert.ToInt32(InitalProgramCounter, 16);
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Parse ProgramCounter into int");
				return;
			}

			int memoryOffset;
			try
			{
				memoryOffset = Convert.ToInt32(MemoryOffset, 16);
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Parse Memory Offset into int");
				return;
			}

			byte[] program;
			try
			{
				program = File.ReadAllBytes(Filename);
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to Open Program Binary");
				return;
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
				return;
			}
			
			Messenger.Default.Send(new NotificationMessage<OpenFileModel>(new OpenFileModel
				                                                              {
					                                                              InitialProgramCounter = programCounter,
																				  MemoryOffset = memoryOffset,
																				  Listing = listing,
																				  Program = program
				                                                              }, "FileLoaded"));

			Close();
		}

		private static void Close()
		{
			Messenger.Default.Send(new NotificationMessage("CloseFileWindow"));
		}

		private void Select()
		{
			var dialog = new OpenFileDialog {DefaultExt = ".bin", Filter = "Binary Assembly (*.bin)|*.bin"};

			var result = dialog.ShowDialog();

			if (result != true)
				return;

			Filename = dialog.FileName;
			RaisePropertyChanged("Filename");
			RaisePropertyChanged("LoadEnabled");
		}
	}
}
