using NecroPanel.ApplicationN.Interfaces;
using NecroPanel.ApplicationN.Models;
using System.Collections.ObjectModel;

namespace NecroPanel.Pages
{

    public partial class DockerPage : ContentPage
    {

        private readonly ISshService _sshService;

        public ObservableCollection<DockerContainerModel> Containers { get; set; } = [];

        public DockerPage(
            ISshService sshService
            )
        {
            InitializeComponent();

            BindingContext = this;

            _sshService = sshService;
            Inicializar();
        }

        private async void Inicializar()
        {
            await ObterContainers();
        }

        public async Task ObterContainers()
        {
            string result =
                await _sshService.EnviarMensagemSSH(
                    "docker ps -a --format \"{{.Names}}|{{.Status}}|{{.Ports}}\"");

            Containers.Clear();
            List<DockerContainerModel> containers = ParseContainers(result);
            foreach(DockerContainerModel container in containers)
            {
                Containers.Add(container);
            }
        }

        private List<DockerContainerModel> ParseContainers(string output)
        {
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    string[] parts = x.Split('|');

                    return new DockerContainerModel
                    {
                        Nome = parts.ElementAtOrDefault(0) ?? "",
                        Status = parts.ElementAtOrDefault(1) ?? "",
                        Ports = parts.ElementAtOrDefault(2) ?? ""
                    };
                })
                .ToList();
        }

        private async void RefreshClicked(object sender, EventArgs e)
        {
            await ObterContainers();
        }

        private async void StartContainerClicked(object sender, EventArgs e)
        {
            DockerContainerModel container = (sender as ImageButton)?.BindingContext as DockerContainerModel;
            await _sshService.EnviarMensagemSSH(
                $"docker start {container.Nome}");

            await DisplayAlertAsync("Comando enviado", $"O comando para iniciar '{container.Nome}' foi enviado. Aguarde um momento e atualize a lista.", "OK");
        }

        private async void StopContainerClicked(object sender, EventArgs e)
        {
            DockerContainerModel container = (sender as ImageButton)?.BindingContext as DockerContainerModel;
            await _sshService.EnviarMensagemSSH(
                $"docker stop {container.Nome}");

            await DisplayAlertAsync("Comando enviado", $"O comando para parar '{container.Nome}' foi enviado. Aguarde um momento e atualize a lista.", "OK");
        }

        private async void DeleteContainerClicked(object sender, EventArgs e)
        {
            DockerContainerModel container = (sender as ImageButton)?.BindingContext as DockerContainerModel;

            bool confirm =
            await DisplayAlertAsync(
                "Deletar Container",
                $"Deseja deletar '{container.Nome}'? Essa ação não pode ser desfeita.",
                "Deletar",
                "Cancelar");

            if (!confirm)
                return;

            await _sshService.EnviarMensagemSSH(
                $"docker rm {container.Nome}");

            await DisplayAlertAsync("Comando enviado", $"O comando para deletar '{container.Nome}' foi enviado. Aguarde um momento e atualize a lista.", "OK");
        }
    }
}