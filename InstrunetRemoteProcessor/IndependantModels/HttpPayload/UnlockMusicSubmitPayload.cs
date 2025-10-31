namespace InstrunetBackend.Server.IndependantModels.HttpPayload
{
    public class UnlockMusicSubmitPayload
    {
        public required string FileInDataUri { get; set; }
        public required string FileName { get; set; }

    }
}
