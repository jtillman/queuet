using System.Threading.Tasks;

namespace QueueT.Notifications
{

    public class PartyEvent
    {
        public string Id { get; }
    }

    public enum PartyNotifications
    {
        [Notification("party.started", typeof(PartyEvent))]
        Started,

        [Notification("party.ended", typeof(PartyEvent))]
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
