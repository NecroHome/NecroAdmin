using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.Settings
{
    public class SettingsProd
    {
        public SettingsProd()
        {
            Preferences.Set("SSH_HOST", "192.168.70.6");
            Preferences.Set("SSH_PORT", 22);
            Preferences.Set("SSH_USER", "necro");
            Preferences.Set("SSH_PASSWORD", "thiago123");
            Preferences.Set("MAC_ADDRESS", "22:20:6f:11:00:b7");
            Preferences.Set("BROADCAST_IP", "192.168.70.255");
        }
    }
}
