using Android.App;
using NecroPanel.ApplicationN.Interfaces;
using NecroPanel.ApplicationN.Models;
using NecroPanel.Pages;
using System.Collections.ObjectModel;

namespace NecroPanel;

public partial class MainPage : ContentPage
{
    private readonly ISshService _sshService;
    private readonly IWakeOnLanService _wakeOnLanService;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    public ObservableCollection<Servico> Servicos { get; set; }

    public MainPage(
        ISshService sshService,
        IWakeOnLanService wakeOnLanService
        )
    {
        InitializeComponent();

        _sshService = sshService;
        _wakeOnLanService = wakeOnLanService;

        Servicos = new ObservableCollection<Servico>
        {
            new()
            {
                Nome = "WoW Auth Server",
                Icone = "wow.png",
                NomeServico = "azeroth-auth",
                MagicPacket = false,
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = () =>ToggleService("azeroth-auth")
            },

            new()
            {
                Nome = "WoW World Server",
                Icone = "wow.png",
                NomeServico = "azeroth-world",
                MagicPacket = false,
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = () => ToggleService("azeroth-world")
            },

            new()
            {
                Nome = "Jellyfin",
                Icone = "jellyfin.png",
                NomeServico = "jellyfin",
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = () => ToggleService("jellyfin")
            },

            new()
            {
                Nome = "MySql",
                Icone = "mysql.png",
                NomeServico = "mysql",
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = () => ToggleService("mysql")
            },

            new()
            {
                Nome = "Arquivos",
                Icone = "folder.png",
                IsService = false,
                Status = StatusServico.NotUsed,
                Callback = FileExplorer
            },

            new()
            {
                Nome = "Docker",
                Icone = "docker.png",
                IsService = false,
                Status = StatusServico.NotUsed,
                Callback = DockerManager
            }
        };

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(1000);

        _cts = new CancellationTokenSource();

        _ = IniciarMonitoramento(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _cts?.Cancel();
        _cts?.Dispose();

        _timer?.Dispose();
    }

    private async Task IniciarMonitoramento(
        CancellationToken token)
    {
        await CheckAlive();

        _timer = new PeriodicTimer(
            TimeSpan.FromSeconds(20));

        try
        {
            while (await _timer.WaitForNextTickAsync(token))
            {
                await CheckAlive();
            }
        }
        catch
        {
            // silent ignore
        }
    }

    private async Task CheckAlive()
    {
        try
        {
            bool servidorOnline =
                await _sshService.VerificarSSH();

            if (servidorOnline)
            {
                semaforoServidor.Background = Colors.LimeGreen;
                lblStatusServidor.Text = "ONLINE";
                btnPowerServidor.Source = "shutdown.png";
            } 
            else
            {
                semaforoServidor.Background = Colors.Red;
                lblStatusServidor.Text = "OFFLINE";
                btnPowerServidor.Source = "poweron.png";
                lblCPU.Text = "0%";
                await pbCPU.ProgressTo(0, 250, Easing.CubicOut);
                lblRAM.Text = "0%";
                await pbRAM.ProgressTo(0, 250, Easing.CubicOut);
                lblTEMP.Text = "0ºC";
                await pbTEMP.ProgressTo(0, 250, Easing.CubicOut);
            }

            if (!servidorOnline)
            {
                foreach (Servico s in Servicos)
                {
                    AtualizarServico(s.NomeServico,StatusServico.Offline);
                }

                return;
            }

            await AtualizarMetricasServidor();

            await AtualizarStatusServicos();
        }
        catch (Exception ex)
        {
            
        }
    }

    private async Task AtualizarMetricasServidor()
    {
        string stats =
            await _sshService.EnviarMensagemSSH(
                "echo CPU=$(top -bn1 | grep 'Cpu(s)' | awk '{print 100 - $8}');" +
                "echo RAM=$(free | awk '/Mem:/ {printf \"%.0f\", $3/$2 * 100}');" +
                "echo TEMP=$(cat /sys/class/thermal/thermal_zone0/temp)");

        var linhas = stats.Split('\n');

        foreach (var linha in linhas)
        {
            if (linha.StartsWith("CPU="))
            {
                string raw =
                    linha.Replace("CPU=", "").Trim();

                if (double.TryParse(raw, out double cpu))
                {
                    cpu = Math.Clamp(cpu, 0, 100);

                    lblCPU.Text = $"{cpu:0}%";

                    await pbCPU.ProgressTo(
                        cpu / 100,
                        250,
                        Easing.CubicOut);
                }
            }

            if (linha.StartsWith("RAM="))
            {
                string raw =
                    linha.Replace("RAM=", "").Trim();

                if (double.TryParse(raw, out double ram))
                {
                    ram = Math.Clamp(ram, 0, 100);

                    lblRAM.Text = $"{ram:0}%";

                    await pbRAM.ProgressTo(
                        ram / 100,
                        250,
                        Easing.CubicOut);
                }
            }

            if (linha.StartsWith("TEMP="))
            {
                string raw =
                    linha.Replace("TEMP=", "").Trim();

                if (double.TryParse(raw, out double temp))
                {
                    temp /= 1000;

                    lblTEMP.Text = $"{temp:0}°C";

                    double normalized =
                        Math.Clamp(temp / 100, 0, 1);

                    await pbTEMP.ProgressTo(
                        normalized,
                        250,
                        Easing.CubicOut);
                }
            }
        }
    }

    private async Task AtualizarStatusServicos()
    {
        string command =
            "echo JELLYFIN=$(systemctl is-active jellyfin);" +
            "echo WOW_AUTH=$(systemctl is-active azeroth-auth);" +
            "echo WOW_WORLD=$(systemctl is-active azeroth-world);" +
            "echo MYSQL=$(systemctl is-active mysql);";

        string resultado =
            await _sshService.EnviarMensagemSSH(command);

        var linhas = resultado.Split('\n');

        foreach (var linha in linhas)
        {
            if (linha.StartsWith("WOW_AUTH"))
            {
                AtualizarServico("azeroth-auth", !linha.EndsWith("inactive") ? StatusServico.Online : StatusServico.Offline);
            }

            if (linha.StartsWith("WOW_WORLD"))
            {
                AtualizarServico("azeroth-world", !linha.EndsWith("inactive") ? StatusServico.Online : StatusServico.Offline);
            }

            if (linha.StartsWith("JELLYFIN="))
            {
                AtualizarServico("jellyfin", !linha.EndsWith("inactive") ? StatusServico.Online : StatusServico.Offline);
            }

            if (linha.StartsWith("MYSQL"))
            {
                AtualizarServico("mysql", !linha.EndsWith("inactive") ? StatusServico.Online : StatusServico.Offline);
            }
        }
    }

    private void AtualizarServico(
        string nomeServico,
        StatusServico status)
    {
        var servico = Servicos.FirstOrDefault(x => x.NomeServico == nomeServico);

        if (servico == null)
        {
            return;
        }

        servico.Status = status;
    }

    private async void ToggleServer(object sender, EventArgs e)
    {
        if (lblStatusServidor.Text == "ONLINE")
        {
            bool confirmar = await DisplayAlertAsync("Desligar Servidor", "Realmente deseja desligar o servidor?", "Desligar Servidor", "Cancelar");
            if (!confirmar)
            {
                return;
            }

            await _sshService.EnviarMensagemSSH("sudo shutdown now");
            await DisplayAlertAsync("Shutdown", "Comando desligar enviado ao servidor.", "OK");
        }
        else
        {
            await _wakeOnLanService.EnviarMagicPacket();
            await DisplayAlertAsync("Poweron", "Pacote Mágico enviado, aguarde a inicialização do servidor.", "OK");

            semaforoServidor.BackgroundColor = Colors.Yellow;
            lblStatusServidor.Text = "INICIANDO";
        }
    }

    private async void OnServicoClicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button)
        {
            return;
        }

        if (button.CommandParameter is not Servico servico)
        {
            return;
        }

        if (servico.Callback == null)
        {
            return;
        }

        try
        {
            if (servico.Nome != "Servidor" && servico.IsService == true)
            {
                servico.Status = StatusServico.Loading;
            }

            Task actionTask = servico.Callback();

            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

            Task completed = await Task.WhenAny(actionTask, timeoutTask);

            if (completed == timeoutTask)
            {
                servico.Status = StatusServico.Error;
                await DisplayAlertAsync("Erro", "O Serviço não foi inicializado dentro do tempo esperado.", "OK");
                return;
            }

            await actionTask;
        }
        catch (Exception ex)
        {
            servico.Status = StatusServico.Error;

            await DisplayAlertAsync("Erro", $"Ocorreu um erro ao executar a ação: {ex.Message}", "OK");
        }
    }

    private async Task ToggleService(string nomeServico)
    {
        string result = await _sshService.EnviarMensagemSSH("systemctl is-active " + nomeServico);
        if (result == "active")
        {
            bool confirmar = await DisplayAlertAsync($"Finalizar {nomeServico}", $"Realmente deseja parar o serviço: {nomeServico}?", "Parar Serviço", "Cancelar");
            if (!confirmar)
            {
                return;
            }

            await _sshService.EnviarMensagemSSH("sudo systemctl stop " + nomeServico);
            await DisplayAlertAsync("Serviço Parado", $"{nomeServico} foi parado com sucesso.", "OK");
        }
        else
        {
            await _sshService.EnviarMensagemSSH("sudo systemctl start " + nomeServico);
            await DisplayAlertAsync("Serviço Iniciado", $"{nomeServico} foi iniciado com sucesso.", "OK");
        }
    }

    private async Task FileExplorer()
    {
        await Navigation.PushAsync(new FileExplorerPage(_sshService, "/home"));
    }

    private async Task DockerManager()
    {
        await Navigation.PushAsync(new DockerPage(_sshService));
    }
}