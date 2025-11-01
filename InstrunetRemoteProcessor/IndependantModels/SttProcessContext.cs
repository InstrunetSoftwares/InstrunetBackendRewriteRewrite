
namespace InstrunetBackend.Server.IndependantModels
{
    public class SttProcessContext : IDisposable
    {
       
        public required string Uuid { get; set;  }
        public required string Email { get; set; }
        public required DateTimeOffset DateTime { get; set; }
        public required LanguageType Language { get; set; }
        public required Task ProcessTask { get; set;  }
        public required byte[] File { get; set;  }
        public required bool CompleteSentence { get; set; }
        public required CancellationTokenSource CancellationToken { get; set; }

        public void Dispose()
        {
            ProcessTask?.Dispose();
        }
    }
}
