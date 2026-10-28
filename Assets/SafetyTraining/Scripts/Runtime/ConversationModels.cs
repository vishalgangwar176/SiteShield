using System;

namespace SafetyTraining.Runtime
{
    [Serializable]
    public sealed class ConversationRequest
    {
        public string siteName;
        public string npcRole;
        public string learnerMessage;
        public string safetyFacts;
        public string progress;
        public string transcript;
    }

    public sealed class ConversationReply
    {
        public ConversationReply(string text, bool usedLanguageModel)
        {
            Text = text;
            UsedLanguageModel = usedLanguageModel;
        }

        public string Text { get; }
        public bool UsedLanguageModel { get; }
    }
}
