# Fixed worker code only. User text/options arrive through the private stdin pipe.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
function Emit([string]$line) { [Console]::Out.WriteLine($line); [Console]::Out.Flush() }
function AudioDuration([string]$path) {
    $reader = [IO.BinaryReader]::new([IO.File]::OpenRead($path))
    try {
        $null = $reader.ReadBytes(12); $byteRate = 0; $dataLength = 0
        while ($reader.BaseStream.Position + 8 -le $reader.BaseStream.Length) {
            $kind = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4)); $length = $reader.ReadUInt32()
            $next = $reader.BaseStream.Position + $length + ($length % 2)
            if ($kind -eq 'fmt ') { $null = $reader.ReadBytes(8); $byteRate = $reader.ReadUInt32() }
            if ($kind -eq 'data') { $dataLength = $length; break }
            $reader.BaseStream.Position = $next
        }
        if ($byteRate -gt 0) { Emit ('WS_TTS|AUDIO|' + [int][Math]::Round(1000.0 * $dataLength / $byteRate)) }
    } finally { $reader.Dispose() }
}
try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ($payload.version -ne 1 -or ([string]$payload.text).Length -gt 16000) { exit 2 }
    $wavePath = [string]$payload.wavePath
    if ($payload.backend -eq 'OneCore') {
        Add-Type -AssemblyName System.Runtime.WindowsRuntime
        $null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Foundation, ContentType=WindowsRuntime]
        $null = [Windows.Storage.Streams.DataReader, Windows.Foundation, ContentType=WindowsRuntime]
        function Await($operation, [Type]$type) {
            $method = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } | Select-Object -First 1
            $task = $method.MakeGenericMethod($type).Invoke($null, @($operation)); $task.GetAwaiter().GetResult()
        }
        $synth = [Activator]::CreateInstance([Type]::GetType('Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows, ContentType=WindowsRuntime'))
        $synth.Options.IncludeWordBoundaryMetadata = $payload.highlight -eq 'word'
        $synth.Options.IncludeSentenceBoundaryMetadata = $payload.highlight -eq 'sentence'
        if ($payload.voiceId) {
                $voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object Id -eq $payload.voiceId | Select-Object -First 1
                if (-not $voice) { exit 3 }; $synth.Voice = $voice
        }
        if (-not $payload.useSystem) {
            $synth.Options.SpeakingRate = [double]$payload.multiplier
        }
        $stream = Await ($synth.SynthesizeTextToStreamAsync([string]$payload.text)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
        try {
            foreach ($track in $stream.TimedMetadataTracks) {
                foreach ($object in $track.Cues) {
                    $cue = [Windows.Media.Core.SpeechCue]$object
                    if ($null -ne $cue.StartPositionInInput) {
                        $start = [int]$cue.StartPositionInInput
                        $length = if ($null -ne $cue.EndPositionInInput -and [int]$cue.EndPositionInInput -ge $start) { 1 + [int]$cue.EndPositionInInput - $start } else { $cue.Text.Length }
                        if ($length -gt 0) { Emit ('WS_TTS|CUE|' + [int][Math]::Round($cue.StartTime.TotalMilliseconds) + '|' + [int][Math]::Round($cue.Duration.TotalMilliseconds) + '|' + $start + '|' + $length) }
                    }
                }
            }
            $reader = [Windows.Storage.Streams.DataReader]::new($stream.GetInputStreamAt(0))
            try {
                if ($stream.Size -gt 134217728) { exit 4 }
                $null = Await ($reader.LoadAsync([uint32]$stream.Size)) ([uint32])
                $bytes = New-Object byte[] ([int]$stream.Size); $reader.ReadBytes($bytes)
                [IO.File]::WriteAllBytes($wavePath, $bytes)
            } finally { $reader.Dispose() }
        } finally { if ($stream -is [IDisposable]) { $stream.Dispose() }; $synth.Dispose() }
    } else {
        Add-Type -AssemblyName System.Speech
        # C# events run during synchronous synthesis without PowerShell runspace
        # callbacks. Map SSML offsets/entities back to the untouched input buffer.
        Add-Type -ReferencedAssemblies System.Speech -TypeDefinition @'
using System;
using System.Globalization;
using System.Security;
using System.Speech.Synthesis;
public static class WordSuggestorSapiWorker {
    public static void Render(string text, string voiceId, string voiceName, bool system, double multiplier, string path, bool cues) {
        using (var synth = new SpeechSynthesizer()) {
            if (!String.IsNullOrEmpty(voiceId)) {
                string name = null;
                foreach (var installed in synth.GetInstalledVoices()) {
                    var info = installed.VoiceInfo;
                    if (installed.Enabled && (String.Equals(info.Id, voiceId, StringComparison.OrdinalIgnoreCase) || voiceId.EndsWith("\\" + info.Id, StringComparison.OrdinalIgnoreCase))) { name = info.Name; break; }
                }
                if (name == null) throw new InvalidOperationException();
                synth.SelectVoice(name);
            }
            var head = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"" + synth.Voice.Culture.Name + "\"><prosody rate=\"" + (multiplier * 100).ToString("0", CultureInfo.InvariantCulture) + "%\">";
            var xml = head + SecurityElement.Escape(text) + "</prosody></speak>";
            if (cues) synth.SpeakProgress += delegate(object sender, SpeakProgressEventArgs e) {
                // Our SSML has one escaped text node. SAPI positions include its
                // prefix plus a decoded UTF-16 offset. CharacterCount can include
                // entity markup; the decoded event Text gives the actual length.
                int start = e.CharacterPosition - (system ? 0 : head.Length), length = e.Text.Length;
                if (start < 0 || length <= 0 || start > text.Length - length || text.Substring(start, length) != e.Text) return;
                Console.WriteLine("WS_TTS|CUE|" + ((int)e.AudioPosition.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) + "|0|" + start + "|" + length); Console.Out.Flush();
            };
            synth.SetOutputToWaveFile(path);
            if (system) synth.Speak(text); else synth.SpeakSsml(xml);
        }
    }
}
'@
        [WordSuggestorSapiWorker]::Render([string]$payload.text, [string]$payload.voiceId, [string]$payload.voiceName, [bool]$payload.useSystem, [double]$payload.multiplier, $wavePath, $payload.highlight -ne 'none')
    }
    AudioDuration $wavePath
    $player = [System.Media.SoundPlayer]::new($wavePath)
    try { $player.Load(); Emit 'WS_TTS|PLAYBACK_START'; $player.PlaySync(); Emit 'WS_TTS|PLAYBACK_END' }
    finally { $player.Dispose() }
    exit 0
} catch {
    # Never return exceptions, SSML, paths or user content to parent diagnostics.
    Emit 'WS_TTS|ERROR'; exit 1
} finally {
    if ($wavePath -and [IO.File]::Exists($wavePath)) { [IO.File]::Delete($wavePath) }
}
