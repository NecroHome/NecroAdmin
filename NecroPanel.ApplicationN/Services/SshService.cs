using NecroPanel.ApplicationN.Interfaces;
using Renci.SshNet;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NecroPanel.ApplicationN.Services
{

    public class SshService : ISshService, IDisposable
    {
        private SshClient? _client;

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private bool _disposed;

        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;

        public SshService()
        {
            _host = Preferences.Get("SSH_HOST", "");
            _port = Preferences.Get("SSH_PORT", 22);
            _user = Preferences.Get("SSH_USER", "");
            _password = Preferences.Get("SSH_PASSWORD", "");

            if (string.IsNullOrWhiteSpace(_host))
                throw new Exception("Host SSH inválido.");
        }

        #region CONNECTION

        private async Task EnsureConnectedAsync()
        {
            if (_client?.IsConnected == true)
                return;

            DisposeClient();

            _client = new SshClient(
                _host,
                _port,
                _user,
                _password);

            await Task.Run(() =>
            {
                _client.Connect();
            });
        }

        private void DisposeClient()
        {
            try
            {
                if (_client != null)
                {
                    if (_client.IsConnected)
                    {
                        _client.Disconnect();
                    }

                    _client.Dispose();
                    _client = null;
                }
            }
            catch
            {
                // silent ignore
            }
        }

        #endregion

        #region EXECUTE

        public async Task<string> EnviarMensagemSSH(string command)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SshService));

            if (string.IsNullOrWhiteSpace(command))
                throw new Exception("Comando inválido.");

            await _semaphore.WaitAsync();

            try
            {
                await EnsureConnectedAsync();

                return await ExecutarComRetry(command);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> ExecutarComRetry(string command)
        {
            try
            {
                return await ExecutarComando(command);
            }
            catch
            {
                // reconecta e tenta novamente
                DisposeClient();

                await EnsureConnectedAsync();

                return await ExecutarComando(command);
            }
        }

        private async Task<string> ExecutarComando(string command)
        {
            return await Task.Run(() =>
            {
                SshCommand cmd = _client!.RunCommand(command);

                if (!string.IsNullOrWhiteSpace(cmd.Error))
                {
                    throw new Exception(cmd.Error);
                }

                return cmd.Result.Trim();
            });
        }

        #endregion

        #region CHECK SSH

        public async Task<bool> VerificarSSH()
        {
            try
            {
                using TcpClient client = new();

                var connectTask = client.ConnectAsync(_host, _port);

                var timeoutTask = Task.Delay(2000);

                var completed =
                    await Task.WhenAny(connectTask, timeoutTask);

                return completed == connectTask &&
                       client.Connected;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region SERVICE STATUS

        public async Task<bool> ChecarEstatusServico(
            string servico,
            bool docker)
        {
            ValidarNomeServico(servico);

            try
            {
                string result;

                if (docker)
                {
                    result = await EnviarMensagemSSH(
                        $"docker inspect -f '{{{{.State.Running}}}}' {servico}");
                }
                else
                {
                    result = await EnviarMensagemSSH(
                        $"systemctl is-active {servico}");
                }

                return result == "true" ||
                       result == "active";
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region TOGGLE SERVICE

        public async Task<bool> AlternarServico(
            string servico,
            bool docker)
        {
            ValidarNomeServico(servico);

            if (docker)
            {
                return await AlternarDocker(servico);
            }

            return await AlternarSystemd(servico);
        }

        private async Task<bool> AlternarDocker(string servico)
        {
            string status =
                await EnviarMensagemSSH(
                    $"docker inspect -f '{{{{.State.Running}}}}' {servico}");

            bool online = status == "true";

            if (online)
            {
                await EnviarMensagemSSH(
                    $"docker stop {servico}");

                return false;
            }

            await EnviarMensagemSSH(
                $"docker start {servico}");

            return true;
        }

        private async Task<bool> AlternarSystemd(string servico)
        {
            string status =
                await EnviarMensagemSSH(
                    $"systemctl is-active {servico}");

            bool online = status == "active";

            if (online)
            {
                await EnviarMensagemSSH(
                    $"sudo systemctl stop {servico}");

                return false;
            }

            await EnviarMensagemSSH(
                $"sudo systemctl start {servico}");

            return true;
        }

        #endregion

        #region VALIDATION

        private void ValidarNomeServico(string servico)
        {
            if (string.IsNullOrWhiteSpace(servico))
            {
                throw new Exception("Nome do serviço inválido.");
            }

            bool valido =
                Regex.IsMatch(
                    servico,
                    @"^[a-zA-Z0-9._-]+$");

            if (!valido)
            {
                throw new Exception(
                    "Nome do serviço contém caracteres inválidos.");
            }
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            DisposeClient();

            _semaphore.Dispose();
        }

        #endregion
    }
}