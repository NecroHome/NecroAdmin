using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.ApplicationN.Interfaces
{
    public interface IWakeOnLanService
    {
        Task<string> EnviarMagicPacket();
    }
}
