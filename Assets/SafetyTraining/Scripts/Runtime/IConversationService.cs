using System.Threading;
using System.Threading.Tasks;

namespace SafetyTraining.Runtime
{
    public interface IConversationService
    {
        Task<ConversationReply> ReplyAsync(ConversationRequest request, CancellationToken cancellationToken);
    }
}
