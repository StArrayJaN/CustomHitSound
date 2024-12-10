namespace CustomHitSound
{
    public interface Languages
    {
        public string selectAudioFile { get; }
        public string enableHitSoundType { get; }
        public string bpmTooHighWarning { get; }
        public string enableBPMLimiter { get; }
        public string BPMLimit { get; }
        
        public class Chinese : Languages
        {
            public string selectAudioFile => "音频文件:";
            public string enableHitSoundType => "启用自定义打击音效";
            public string bpmTooHighWarning => "BPM过高!";
            public string enableBPMLimiter => "启用BPM限制，一般情况下，如果BPM过高，可能会导致游戏卡死";
            public string BPMLimit => "最小BPM限制(必须为数字)";
            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public class Korean : Languages
        {
            public string selectAudioFile => "오디오 파일:";
            public string enableHitSoundType => "사운드 유형";
            public string bpmTooHighWarning => "BPM이 너무 높습니다!";
            public string enableBPMLimiter => "BPM 제한을 활성화합니다. 일반적으로 BPM이 너무 높으면 게임이 끊길 수 있습니다.";
            public string BPMLimit => "최소 BPM 제한(숫자여야 함)";
            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public class English : Languages
        {
            public string selectAudioFile => "Audio File:";
            public string enableHitSoundType => "Enable Custom Hits Sound";
            public string bpmTooHighWarning => "BPM is too high!";
            public string enableBPMLimiter => "Enable BPM Limiter, usually if BPM is too high, it may cause the game to crash";
            public string BPMLimit => "Minimum BPM Limit (must be a number)";
            public override string ToString()
            {
                return GetType().Name;
            }
        }
    }
}