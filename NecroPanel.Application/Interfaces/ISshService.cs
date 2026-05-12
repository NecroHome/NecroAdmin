using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.Application.Interfaces
{
    public interface ISshService
    {
        string EnviarMensagemSSH(string mensagem);
    }
}
