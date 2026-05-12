using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.ApplicationN.Models
{
    public partial class DockerContainerModel : ObservableObject
    {
        [ObservableProperty]
        private string nome;

        [NotifyPropertyChangedFor(nameof(Online))]
        [NotifyPropertyChangedFor(nameof(Offline))]
        [ObservableProperty]
        private string status;

        [ObservableProperty]
        private string ports;

        public bool Online =>
            Status?.Contains("Up") == true;

        public bool Offline =>
            !Online;
    }
}
