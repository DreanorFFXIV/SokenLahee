using System;
using Dalamud.Plugin;
using ImGuiNET;
using SoundSetter;

namespace SokenLahee
{
    public class PluginUI
    {
        private readonly VolumeControls _volumeControls;
        public bool IsVisible { get; set; }

        public PluginUI(VolumeControls volumeControls)
        {
            _volumeControls = volumeControls;
        }
        
        public void Draw()
        {
            if (!IsVisible)
            {
                return;
            }

            var pVisible = IsVisible;
            if (_volumeControls.BaseAddress == IntPtr.Zero)
            {
                ImGui.Begin("SokenLahee", ref pVisible, ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text("This appears to be your first installation of this plugin (or you reloaded all of your plugins).\nPlease manually change a volume setting once in order to initialize the plugin.");
                ImGui.End();
            }
            
            IsVisible = pVisible;
        }
    }
}