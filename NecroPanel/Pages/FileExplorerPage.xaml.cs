using NecroPanel.ApplicationN.Interfaces;
using NecroPanel.ApplicationN.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NecroPanel.Pages
{ 
	public partial class FileExplorerPage : ContentPage
	{
		private readonly ISshService _sshService;
        public ObservableCollection<ArquivoItemModel> Arquivos { get; set; } = [];

        private string _caminhoAtual;
        public string CaminhoAtual
        {
            get => _caminhoAtual;
            set
            {
                _caminhoAtual = value;
                OnPropertyChanged();
                Titulo = value;
            }
        }

        private string _titulo;
        public string Titulo
        {
            get
            {
                if (String.IsNullOrEmpty(CaminhoAtual))
                {
                    return "/";
                }

                if (CaminhoAtual.Length <= 26)
                {
                    return CaminhoAtual;
                }

                return "..." + CaminhoAtual.Substring(CaminhoAtual.Length - 26);
            }

            set
            {
                _titulo = value;
                OnPropertyChanged();
            }
        }

        private bool _diretorioAtualTemCompose;

        public bool DiretorioAtualTemCompose
        {
            get => _diretorioAtualTemCompose;
            set
            {
                _diretorioAtualTemCompose = value;
                OnPropertyChanged();
            }
        }

        private bool _diretorioAtualEhGit;

        public bool DiretorioAtualEhGit
        {
            get => _diretorioAtualEhGit;
            set
            {
                _diretorioAtualEhGit = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand AbrirItemCommand { get; set; }
        public ICommand GitPullCommand { get; set; }
        public ICommand DockerComposeCommand { get; set; }

        public FileExplorerPage(
			ISshService sshService,
            string caminho
			)
		{
			InitializeComponent();
            BindingContext = this;
			
			_sshService = sshService;
            CaminhoAtual = caminho;

            AbrirItemCommand = new Command<ArquivoItemModel>(async (item) =>
            {
                if (!item.IsDiretorio)
                {
                    return;
                }

                await Navigation.PushAsync(new FileExplorerPage(_sshService, item.Caminho));
            });

            Inicializar(caminho);
		}

        private async void Inicializar(string caminho)
        {
            List<ArquivoItemModel> arquivos = await ListarArquivos(caminho);
            foreach (var arquivo in arquivos)
            {
                Arquivos.Add(arquivo);
            }
        }

        public async Task<List<ArquivoItemModel>> ListarArquivos(string caminho)
        {
            var lista = new List<ArquivoItemModel>();

            string comando =
                $"cd \"{caminho}\"; " +
                "docker=false; " +
                "git=false; " +
                "if [ -f \"docker-compose.yml\" ]; then docker=true; fi; " +
                "if [ -d \".git\" ]; then git=true; fi; " +
                "echo \"INFO|$docker|$git\"; " +
                "for item in *; do " +
                "if [ -d \"$item\" ]; then tipo=\"DIR\"; else tipo=\"FILE\"; fi; " +
                "echo \"$item|$tipo\"; " +
                "done";

            var resultado = await _sshService.EnviarMensagemSSH(comando);

            var linhas = resultado
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var linha in linhas)
            {
                var partes = linha.Split('|');

                if (partes[0] == "INFO")
                {
                    DiretorioAtualTemCompose = bool.Parse(partes[1]);
                    DiretorioAtualEhGit = bool.Parse(partes[2]);

                    continue;
                }

                Arquivos.Add(new ArquivoItemModel
                {
                    Nome = partes[0],
                    Caminho = $"{caminho}/{partes[0]}",
                    IsDiretorio = partes[1] == "DIR"
                });
            }

            return lista
                .OrderByDescending(x => x.IsDiretorio)
                .ThenBy(x => x.Nome)
                .ToList();
        }

        private async void GitPullClicked(object sender, EventArgs e)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                    "Git Pull",
                    "Deseja executar git pull?",
                    "Sim",
                    "Cancelar");

            if (!confirm)
                return;

            try
            {
                IsLoading = true;

                string comando =
                    $"cd \"{CaminhoAtual}\"; " +
                    "git pull";

                var resultado =
                    await _sshService.EnviarMensagemSSH(comando);

                resultado = resultado?.Trim();

                if (string.IsNullOrWhiteSpace(resultado))
                {
                    resultado = "Git: Concluído.";
                }
                else if (resultado.Contains("Already up to date"))
                {
                    resultado = "Git: Up to date!";
                }
                else if (resultado.Contains("Fast-forward"))
                {
                    resultado = "Git: Repositório atualizado.";
                }
                else
                {
                    resultado = "Git pull concluído.";
                }

                IsLoading = false;

                await Shell.Current.DisplayAlertAsync("Git Pull", resultado, "Ok");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Git Pull", ex.Message, "Ok");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void DockerComposeClicked(object sender, EventArgs e)
        {
            string result = await Shell.Current.DisplayActionSheetAsync(
                "Docker Compose",
                "Cancelar",
                null,
                "docker compose up -d",
                "docker compose up -d --build");

            if (result == "Cancelar" || string.IsNullOrWhiteSpace(result))
                return;

            try
            {
                IsLoading = true;

                await Shell.Current.DisplayAlertAsync(
                    "Docker",
                    "Iniciando compose...",
                    "OK");

                string comando =
                    $"cd \"{CaminhoAtual}\"; " +
                    result;

                string retorno =
                    await _sshService.EnviarMensagemSSH(comando);

                retorno = retorno?.Trim();

                if (string.IsNullOrWhiteSpace(retorno))
                {
                    retorno = "Docker Compose concluído.";
                }

                if (retorno.Length > 300)
                {
                    retorno = retorno[..300] + "...";
                }

                await Shell.Current.DisplayAlertAsync(
                    "Docker Compose",
                    retorno,
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Erro",
                    ex.Message,
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void FecharExploradorClicked(object sender, EventArgs e)
        {
            await Navigation.PopToRootAsync();
        }
    }
}