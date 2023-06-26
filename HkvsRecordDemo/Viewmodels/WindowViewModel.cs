using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HkvsRecordDemo.DataClass;

namespace HkvsRecordDemo.Viewmodels
{
    public class WindowViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CameraInfo> _CameraInfos;

        public ObservableCollection<CameraInfo> CameraInfos
        {
            get
            {
                return _CameraInfos;
            }
            set
            {
                _CameraInfos = value;
                OnPropertyChanged("CameraInfos");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
