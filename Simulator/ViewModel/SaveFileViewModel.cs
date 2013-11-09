using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Simulator.Model;

namespace Simulator.ViewModel
{
	/// <summary>
	/// The ViewModel Used by the SaveFileView
	/// </summary>
	public class SaveFileViewModel : ViewModelBase
	{
		private readonly StateFileModel _stateFileModel;
		
		#region Properties
		/// <summary>
		/// The Relay Command called when saving a file
		/// </summary>
		public RelayCommand SaveFileCommand { get; set; }
		
		/// <summary>
		/// The Relay Command called when closing a file
		/// </summary>
		public RelayCommand CloseCommand { get; set; }

		/// <summary>
		/// The Relay Command called when Selecting a file
		/// </summary>
		public RelayCommand SelectFileCommand { get; set; }

		/// <summary>
		/// The file to be saved
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// Tells the UI that that a file has been selected and can be saved.
		/// </summary>
		public bool SaveEnabled { get { return !string.IsNullOrEmpty(Filename); }}
		#endregion

		#region Public Methods
		/// <summary>
		/// Instantiates a new instance of the SaveFileViewModel. This is used by the IOC to create the default instance.
		/// </summary>
		[PreferredConstructor]
		public SaveFileViewModel()
		{

		}

		/// <summary>
		/// Instantiates a new instance of the SaveFileViewModel
		/// </summary>
		/// <param name="stateFileModel">The StateFIleModel to be serialized to a file</param>
		public SaveFileViewModel(StateFileModel stateFileModel)
		{
			SaveFileCommand = new RelayCommand(Save);
			CloseCommand = new RelayCommand(Close);
			SelectFileCommand = new RelayCommand(Select);
			_stateFileModel = stateFileModel;
		}
		#endregion

		#region Private Methods
		private void Save()
		{
			var formatter = new BinaryFormatter();
			Stream stream = new FileStream(Filename, FileMode.Create, FileAccess.Write, FileShare.None);
			formatter.Serialize(stream, _stateFileModel);
			stream.Close();

			Close();
		}

		private static void Close()
		{
			Messenger.Default.Send(new NotificationMessage("CloseSaveFileWindow"));
		}

		private void Select()
		{
			var dialog = new SaveFileDialog { DefaultExt = ".6502", Filter = "6502 Simulator Save State (*.6502)|*.6502" };

			var result = dialog.ShowDialog();

			if (result != true)
				return;

			Filename = dialog.FileName;
			RaisePropertyChanged("Filename");
			RaisePropertyChanged("SaveEnabled");

		}
		#endregion
	}
}
