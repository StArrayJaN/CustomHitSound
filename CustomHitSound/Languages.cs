namespace CustomHitSound
{
    public interface Languages
    {
        public string selectAudioFile { get; }
        public string enableHitSoundType { get; }
        public string selectFileHint { get; }
        
        public class Chinese : Languages
        {
            public string selectAudioFile => "音频文件:";
            public string enableHitSoundType => "启用自定义打击音效";
            public string selectFileHint => "选择一个音频文件...";
            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public class Korean : Languages
        {
            public string selectAudioFile => "오디오 파일:";
            public string enableHitSoundType => "사운드 유형";
            public string selectFileHint => "음원 파일을 선택하세요...";
            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public class English : Languages
        {
            public string selectAudioFile => "Audio File:";
            public string enableHitSoundType => "Enable Custom Hit Sound";
            public string selectFileHint => "Select an audio file...";
            public override string ToString()
            {
                return GetType().Name;
            }
        }
    }
}