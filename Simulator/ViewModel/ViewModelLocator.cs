/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:Simulator"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace Simulator.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainViewModel>();
			SimpleIoc.Default.Register<OpenFileViewModel>();
			SimpleIoc.Default.Register<SaveFileViewModel>();
        }

        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }

		public OpenFileViewModel OpenFile
		{
			get { return ServiceLocator.Current.GetInstance<OpenFileViewModel>(); }
		}

	    public SaveFileViewModel SaveFile
	    {
		    get { return ServiceLocator.Current.GetInstance<SaveFileViewModel>(); }
	    }
        
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}