using System.Threading.Tasks;

namespace QueueT.Notifications
{

    public class PartyEvent
    {
        public string Id { get; }
    }

    public enum PartyNotifications
    {
        [TopicAttribute("party.started", typeof(PartyEvent))]
        Started,

        [TopicAttribute("party.ended", typeof(PartyEvent))]
        Ended
    }

    public class SomeClass
    {
        [Subscription(
            PartyNotifications.Ended,
            Queue = "default")]
        public async Task ReactToStuff(string id)
        {
        }
    }
}
