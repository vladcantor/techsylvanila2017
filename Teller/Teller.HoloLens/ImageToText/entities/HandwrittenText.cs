using System.Collections.Generic;

namespace CognitiveServices.ImageToText.entities
{
    public class LineWithText
    {
        public string text { get; set; }

        public override string ToString()
        {
            return text;
        }
    }

    public class RecognitionResult
    {
        public List<LineWithText> lines { get; set; }
    }

    public class LinesWithText
    {
        public RecognitionResult recognitionResult { get; set; }

    }
}
