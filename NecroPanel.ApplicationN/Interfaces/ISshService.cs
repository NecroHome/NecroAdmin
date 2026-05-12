namespace NecroPanel.ApplicationN.Interfaces;

public interface ISshService
{
    Task<string> EnviarMensagemSSH(string mensagem);

    Task<bool> VerificarSSH();

    Task<bool> AlternarServico(
        string servico,
        bool docker);

    Task<bool> ChecarEstatusServico(
        string servico,
        bool docker);
}