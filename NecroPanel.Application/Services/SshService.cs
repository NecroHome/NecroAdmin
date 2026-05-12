using NecroPanel.Application.Interfaces;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.Application.Services
{
    public class SshService : ISshService, IDisposable
    {
        private SshClient? _client;
        private readonly object _lock = new object();

        public string EnviarMensagemSSH(string command)
        {
            lock(_lock)
            {
                try
                {
                    if (_client == null || _client.IsConnected)
                    {
                        string host = Preferences.Get("SSH_HOST", "");
                        int port = Preferences.Get("SSH_PORT", 22);
                        string user = Preferences.Get("SSH_USER", "");
                        string pasw = Preferences.Get("SSH_PASSWORD", "");

                        if (string.IsNullOrEmpty(host))
                        {
                            throw new Exception("Host SSH Inválido");
                        }

                        _client?.Dispose();
                        _client = new SshClient(host, port, user, pasw);
                        _client.Connect();
                    }

                    SshCommand cmd = _client.RunCommand(command);
                    if (!string.IsNullOrEmpty(cmd.Error))
                    {
                        return $"ERRO: {cmd.Error}";
                    }

                    return cmd.Result;
                }
                catch (Exception ex)
                {
                    return $"ERRO: {ex.Message}";
                }
            }
        }

        public void Dispose()
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
                }
            }
            catch (Exception ex)
            {
                // Silent Ignore Dispose Erros
            }
        }
    }
}
