using CommunityToolkit.Mvvm.ComponentModel;

namespace NecroPanel.ApplicationN.Models
{
    public partial class Servico : ObservableObject
    {
        [ObservableProperty]
        private string nome;

        [ObservableProperty]
        private string icone;

        [ObservableProperty]
        private string nomeServico;

        [ObservableProperty]
        private bool magicPacket;

        [ObservableProperty]
        private bool docker;

        [ObservableProperty]
        private StatusServico status;

        [ObservableProperty]
        private bool isService;

        public Func<Task> Callback { get; set; }

        public bool IsLoading =>
            Status == StatusServico.Loading;

        public Color StatusColor =>
            Status switch
            {
                StatusServico.Online => Colors.LimeGreen,

                StatusServico.Loading => Colors.Gold,

                StatusServico.Error => Colors.OrangeRed,

                _ => Colors.Red
            };

        partial void OnStatusChanged(StatusServico value)
        {
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Icone));
            OnPropertyChanged(nameof(IsService));
        }
    }
}