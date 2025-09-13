namespace InstrunetBackend.Server.IndependantModels.HttpPayload
{
    public class SttUploadPayload
    {
        
        public required string File { get; set; }
        public required LanguageType Language { get; set; }
        public required string Email { get; set; }
        public required bool CompleteSentence { get; set; }

    }
}
