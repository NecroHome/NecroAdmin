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
                Nome = "Servidor",
                Icone = "shutdown.png",
                NomeServico = "server",
                MagicPacket = true,
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = ToggleServidor
            },

            new()
            {
                Nome = "WoW Auth Server",
                Icone = "wow.png",
                NomeServico = "azeroth-auth",
                MagicPacket = false,
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = ToggleWowAuth
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
                Callback = ToggleWowWorld
            },

            new()
            {
                Nome = "Jellyfin",
                Icone = "jellyfin.png",
                NomeServico = "jellyfin",
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = ToggleJellyfin
            },

            new()
            {
                Nome = "QBitTorrent",
                Icone = "qbittorrent.png",
                NomeServico = "qbittorrent",
                Docker = true,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = ToggleQBit
            },

            new()
            {
                Nome = "NecroFinances",
                Icone = "necrofinances.png",
                NomeServico = "necrofinances",
                Docker = true,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = ToggleNecrofinances
            },

            new()
            {
                Nome = "MySql",
                Icone = "mysql.png",
                NomeServico = "mysql",
                Docker = false,
                Status = StatusServico.Loading,
                IsService = true,
                Callback = ToggleMySql
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

            AtualizarServico(
                "server",
                servidorOnline
                    ? StatusServico.Online
                    : StatusServico.Offline);

            if (!servidorOnline)
            {
                await Log(
                    "Servidor Offline, próximo check em 20 segundos...");

                foreach (Servico s in Servicos)
                {
                    AtualizarServico(
                        s.NomeServico,
                        StatusServico.Offline);
                }

                return;
            }

            await AtualizarMetricasServidor();

            await AtualizarStatusServicos();
        }
        catch (Exception ex)
        {
            await Log(ex.Message);
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
            "echo QBIT=$(docker inspect -f '{{.State.Running}}' qbittorrent);" +
            "echo ANGULAR=$(docker inspect -f '{{.State.Running}}' angular-app);" +
            "echo API=$(docker inspect -f '{{.State.Running}}' necro_api);" +
            "echo DB=$(docker inspect -f '{{.State.Running}}' mariadb_local);" +
            "echo WOW_AUTH=$(systemctl is-active azeroth-auth);" +
            "echo WOW_WORLD=$(systemctl is-active azeroth-world);";

        string resultado =
            await _sshService.EnviarMensagemSSH(command);

        var linhas = resultado.Split('\n');

        bool frontend = false;
        bool api = false;
        bool db = false;

        foreach (var linha in linhas)
        {
            if (linha.StartsWith("WOW_AUTH"))
            {
                AtualizarServico("azeroth-auth",
                    !linha.EndsWith("inactive")
                    ? StatusServico.Online
                    : StatusServico.Offline);
            }

            if (linha.StartsWith("WOW_WORLD"))
            {
                AtualizarServico("azeroth-world",
                    !linha.EndsWith("inactive")
                    ? StatusServico.Online
                    : StatusServico.Offline);
            }

            if (linha.StartsWith("JELLYFIN="))
            {
                AtualizarServico(
                    "jellyfin",
                    !linha.EndsWith("inactive")
                        ? StatusServico.Online
                        : StatusServico.Offline);
            }

            if (linha.StartsWith("QBIT="))
            {
                AtualizarServico(
                    "qbittorrent",
                    linha.Contains("true")
                        ? StatusServico.Online
                        : StatusServico.Offline);
            }

            if (linha.StartsWith("ANGULAR="))
            {
                frontend = linha.Contains("true");
            }

            if (linha.StartsWith("API="))
            {
                api = linha.Contains("true");
            }

            if (linha.StartsWith("DB="))
            {
                db = linha.Contains("true");
            }
        }

        AtualizarServico(
            "necrofinances",
            frontend && api && db
                ? StatusServico.Online
                : StatusServico.Offline);
    }

    private void AtualizarServico(
        string nomeServico,
        StatusServico status)
    {
        var servico =
            Servicos.FirstOrDefault(
                x => x.NomeServico == nomeServico);

        if (servico == null)
        {
            return;
        }

        servico.Status = status;
    }

    private async void OnServicoClicked(
    object sender,
    EventArgs e)
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

            Task actionTask =
                servico.Callback();

            Task timeoutTask =
                Task.Delay(TimeSpan.FromSeconds(10));

            Task completed =
                await Task.WhenAny(
                    actionTask,
                    timeoutTask);

            if (completed == timeoutTask)
            {
                servico.Status = StatusServico.Error;

                await Log(
                    $"{servico.Nome} timeout.");

                return;
            }

            await actionTask;
        }
        catch (Exception ex)
        {
            servico.Status = StatusServico.Error;

            await Log(ex.Message);
        }
    }

    private async Task ToggleServidor()
    {
        var server =
            Servicos.First(
                x => x.NomeServico == "server");

        if (server.Status == StatusServico.Online)
        {
            await Log(
                await _sshService.EnviarMensagemSSH(
                    "sudo shutdown now"));
        }
        else
        {
            await Log(
                await _wakeOnLanService.EnviarMagicPacket());
        }
    }

    private async Task ToggleJellyfin()
    {
        bool result =
            await _sshService.AlternarServico(
                "jellyfin",
                false);

        AtualizarServico(
            "jellyfin",
            result
                ? StatusServico.Online
                : StatusServico.Offline);

        await Log(
            result
                ? "Jellyfin Inicializado!"
                : "Jellyfin Finalizado!");
    }

    private async Task ToggleMySql()
    {
        bool result =
            await _sshService.AlternarServico(
                "mysql",
                false);

        AtualizarServico(
            "mysql",
            result
                ? StatusServico.Online
                : StatusServico.Offline);

        await Log(
            result
                ? "MySql Inicializado!"
                : "MySql Finalizado!");
    }

    private async Task ToggleQBit()
    {
        bool result =
            await _sshService.AlternarServico(
                "qbittorrent",
                true);

        AtualizarServico(
            "qbittorrent",
            result
                ? StatusServico.Online
                : StatusServico.Offline);

        await Log(
            result
                ? "QBit Inicializado!"
                : "QBit Finalizado!");
    }

    private async Task ToggleWowAuth()
    {
        bool result =
            await _sshService.AlternarServico(
                "azeroth-auth",
                false);

        AtualizarServico(
            "azeroth-auth",
            result
                ? StatusServico.Online
                : StatusServico.Offline);

        await Log(
            result
                ? "WoW Auth Inicializado!"
                : "WoW Auth Finalizado!");
    }

    private async Task ToggleWowWorld()
    {
        bool result =
            await _sshService.AlternarServico(
                "azeroth-world",
                false);

        AtualizarServico(
            "azeroth-world",
            result
                ? StatusServico.Online
                : StatusServico.Offline);

        await Log(
            result
                ? "WoW World Inicializado!"
                : "WoW World Finalizado!");
    }

    private async Task ToggleNecrofinances()
    {
        bool result1 =
            await _sshService.AlternarServico(
                "angular-app",
                true);

        bool result2 =
            await _sshService.AlternarServico(
                "necro_api",
                true);

        bool result3 =
            await _sshService.AlternarServico(
                "mariadb_local",
                true);

        bool online =
            result1 &&
            result2 &&
            result3;

        AtualizarServico(
            "necrofinances",
            online
                ? StatusServico.Online
                : StatusServico.Offline);

        await Log(
            online
                ? "NecroFinances Inicializado!"
                : "NecroFinances Finalizado!");
    }

    private async void OnExecutarComandoClicked(
    object sender,
    EventArgs e)
    {
        try
        {
            string command =
                txtCommand.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            await Log($"> {command}");

            string result =
                await _sshService.EnviarMensagemSSH(command);

            await Log(result);

            txtCommand.Text = "";
        }
        catch (Exception ex)
        {
            await Log(ex.Message);
        }
    }

    private async Task Log(string message)
    {
        lblOutput.Text +=
            $"\n[{DateTime.Now:HH:mm:ss}] {message}";

        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await outputScroll.ScrollToAsync(
                    lblOutput,
                    ScrollToPosition.End,
                    true);
            });
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