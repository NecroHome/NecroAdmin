using System;
using System.Collections.Generic;
using System.Text;

namespace NecroPanel.Settings
{
    public class SettingsDev
    {
        public SettingsDev()
        {
            Preferences.Set("SSH_HOST", "");
            Preferences.Set("SSH_PORT", 22);
            Preferences.Set("SSH_USER", "");
            Preferences.Set("SSH_PASSWORD", "");
            Preferences.Set("MAC_ADDRESS", "");
            Preferences.Set("BROADCAST_IP", "");
        }
    }
}
