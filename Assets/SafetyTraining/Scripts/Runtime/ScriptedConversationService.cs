using System.Threading;
using System.Threading.Tasks;

namespace SafetyTraining.Runtime
{
    public sealed class ScriptedConversationService : IConversationService
    {
        public Task<ConversationReply> ReplyAsync(
            ConversationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = $"I am the {request.npcRole} for {request.siteName}. " +
                       $"Current progress is {request.progress}. Focus on this verified guidance: {request.safetyFacts}";
            return Task.FromResult(new ConversationReply(text, false));
        }
    }
}
