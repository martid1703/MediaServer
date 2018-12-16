using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaClient
{
    public class StatusUpdate:INotifyPropertyChanged
    {
        private string message;
        private double progress;
        private double min;
        private double max;

        public event PropertyChangedEventHandler PropertyChanged;

        public StatusUpdate()
        {
            Min = 0;
            Max = 100;
        }

        public Double Min
        {
            get { return this.min; }
            set
            {
                this.min = value;
                this.OnPropertyChanged("Min");
            }
        }

        public Double Max
        {
            get { return this.max; }
            set
            {
                this.max = value;
                this.OnPropertyChanged("Max");
            }
        }

        public string Message
        {
            get { return this.message; }
            set
            {
                this.message = value;
                this.OnPropertyChanged("Message");
            }
        }

        public Double Progress
        {
            get { return this.progress; }
            set
            {
                this.progress = value;
                this.OnPropertyChanged("Progress");
            }
        }

        void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
