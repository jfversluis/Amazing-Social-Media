using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Plugin.Media.Abstractions;

namespace AmazingSocialMedia.Services
{
    public class ContentModeratorService
    {
		private const double ProbabilityThreshold = 0.5;
        private CustomVisionPredictionClient customVisionPredictionClient;
        private ContentModeratorClient _client;
        private FaceAPI _faceApi;

        private void InitIfRequired()
        {
            if (_client == null)
            {
                var credentials = new ApiKeyServiceClientCredentials(ApiKeys.ContentModeratorKey);
                _client = new ContentModeratorClient(credentials);
            }

            if (_faceApi == null)
            {
                var credentials = new ApiKeyServiceClientCredentials(ApiKeys.FaceApiKey);
                _faceApi = new FaceAPI(credentials);
                _faceApi.AzureRegion = ApiKeys.FaceApiRegion;
            }

            if (customVisionPredictionClient == null)
                customVisionPredictionClient = new CustomVisionPredictionClient { ApiKey = ApiKeys.PredictionKey, Endpoint = "https://jfversluis-customvisionsample.cognitiveservices.azure.com/" };
        }

        public async Task<bool> IsFace(MediaFile photo)
        {
            InitIfRequired();
            if (photo == null) return false;

            using (var stream = photo.GetStreamWithImageRotatedForExternalStorage())
            {
                var result = await new FaceOperations(_faceApi).DetectInStreamAsync(stream);
                return result != null && result.Any();
            }
        }

        public async Task<bool> IsDuckFace(MediaFile photo)
        {
            InitIfRequired();
            if (photo == null) return false;

            using (var stream = photo.GetStreamWithImageRotatedForExternalStorage())
            {
                var predictionModels = await customVisionPredictionClient.ClassifyImageAsync(ApiKeys.ProjectId, "Iteration1", stream);
                return predictionModels.Predictions
                                       .FirstOrDefault(p => p.TagName == "duckface")
                                       .Probability > ProbabilityThreshold;
            }
        }

        public async Task<bool> ContainsProfanity(string text)
        {
            InitIfRequired();
            if (string.IsNullOrEmpty(text)) return false;

            using (var textStream = GenerateStreamFromString(text))
            {
                var lang = await _client.TextModeration.DetectLanguageAsync("text/plain", textStream);
                var moderation = await _client.TextModeration.ScreenTextAsync("text/plain", textStream, lang.DetectedLanguageProperty);
                return moderation.Terms != null && moderation.Terms.Any();
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
