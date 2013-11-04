using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Simulator.Model;
using Proc = Processor.Processor;

namespace Simulator.ViewModel
{
	public class SaveFileViewModel : ViewModelBase
	{
		private SaveFileModel _saveFileModel;
		
		public RelayCommand SaveFileCommand { get; set; }

		public RelayCommand CloseCommand { get; set; }

		public RelayCommand SelectFileCommand { get; set; }

		public string Filename { get; set; }

		public bool SaveEnabled { get { return !string.IsNullOrEmpty(Filename); }}

		[PreferredConstructor]
		public SaveFileViewModel()
		{
			
		}

		public SaveFileViewModel(SaveFileModel saveFileModel)
		{
			SaveFileCommand = new RelayCommand(Save);
			CloseCommand = new RelayCommand(Close);
			SelectFileCommand = new RelayCommand(Select);
			_saveFileModel= saveFileModel;
		}
		
		private void Save()
		{
			var formatter = new BinaryFormatter();
			Stream stream = new FileStream(Filename, FileMode.Create, FileAccess.Write, FileShare.None);
			formatter.Serialize(stream, _saveFileModel);
			stream.Close();

			Close();
		}
		
		private static void Close()
		{
			Messenger.Default.Send(new NotificationMessage("CloseSaveFileWindow"));
		}

		private void Select()
		{
			var dialog = new SaveFileDialog {DefaultExt = ".6502", Filter = "6502 Simulator Save State (*.6502)|*.6502"};

			var result = dialog.ShowDialog();

			if (result != true)
				return;

			Filename = dialog.FileName;
			RaisePropertyChanged("Filename");
			RaisePropertyChanged("SaveEnabled");

		}
	}
}
