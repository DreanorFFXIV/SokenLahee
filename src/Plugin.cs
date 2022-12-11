using System;
using System.IO;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using NAudio.Wave;
using SoundSetter;

namespace SokenLahee
{
    public class Plugin : IDalamudPlugin
    {

        public string Name => "SokenLahee";

        [PluginService] 
        private ClientState _clientState { get; set; }
        
        [PluginService] 
        private SigScanner _sigScanner { get; set; }
        
        private readonly byte[] _soundBytes;
        private WaveOutEvent _soundPlayer;
        private uint _originalBgmVolume;
        private readonly VolumeControls _vc;      
        private ushort _lastTerritory;

        public Plugin()
        {
            _soundPlayer = new WaveOutEvent();
            
            _vc = new VolumeControls(_sigScanner, null);
            
            var _localDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _soundBytes = File.ReadAllBytes(Path.Combine(_localDir, "soken.wav"));
            _clientState.TerritoryChanged += OnTerritoryChanged;
        }

        public void Dispose()
        {
            _soundPlayer.Pause();
            _soundPlayer.Stop();
            _soundPlayer.Dispose();
            _clientState.TerritoryChanged -= OnTerritoryChanged;
        }

        private void OnTerritoryChanged(object sender, ushort territory)
        {
            PluginLog.Log("lt:" + _lastTerritory);
            if (_lastTerritory == 817)
            {
                UnmuteBgm();
                _soundPlayer.Pause();
                _soundPlayer.Stop();
            }
            
            if (territory == 817) //817 is Rak'tika
            {
                var volume = GetSfxVolume();
                if (volume > 0)
                {
                    MuteBgm();
                    PlaySong(volume);
                }
            }
            
            _lastTerritory = territory;
        }

        private void UnmuteBgm()
        {
            VolumeControls.ToggleVolume(_vc.BgmMuted, OperationKind.Unmute);
        }

        private void MuteBgm()
        {
            VolumeControls.ToggleVolume(_vc.BgmMuted, OperationKind.Mute);
        }

        private void PlaySong(float volume)
        {
            var waveStream = new WaveFileReader(new MemoryStream(_soundBytes));
            var volumeStream = new WaveChannel32(waveStream);
            volumeStream.Volume = volume;

            _soundPlayer.Init(volumeStream);
            _soundPlayer.PlaybackStopped += (snd, evn) =>
            {
                PluginLog.Log("state:" + _clientState.TerritoryType );
                if (_clientState.TerritoryType == 817)
                {
                    PlaySong(volume);
                }
            };
            _soundPlayer.Play();
        }

        //GetSfxVolume from Fool22
        private unsafe float GetSfxVolume()
        {
            try
            {
                var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;

                var seEnabled = false;
                var masterEnabled = false;
                var masterVolume = 0u;

                for (var i = 0; i < configBase.ConfigCount; i++)
                {
                    var entry = configBase.ConfigEntry[i];

                    if (entry.Name != null)
                    {
                        var name = MemoryHelper.ReadStringNullTerminated(new IntPtr(entry.Name));

                        if (name == "IsSndBgm")
                        {
                            _originalBgmVolume = entry.Value.UInt;
                        }
                        
                        if (name == "IsSndSe")
                        {
                            var value = entry.Value.UInt;

                            seEnabled = value == 0;
                        }

                        if (name == "IsSndMaster")
                        {
                            var value = entry.Value.UInt;

                            masterEnabled = value == 0;
                        }

                        if (name == "SoundMaster")
                        {
                            var value = entry.Value.UInt;

                            masterVolume = value;
                        }
                    }
                }

                if (!(seEnabled && masterEnabled))
                    return 0;

                var vol = masterVolume / 100F;
                return Math.Clamp(vol, 0f, 1f);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
    }
}
