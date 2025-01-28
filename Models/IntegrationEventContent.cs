namespace SystemAdmin.Models
{
    public class IntegrationEventContent
    {
        public int TotalRecords { get; set; }
        public List<RsiPostItem> Data { get; set; }
    }

    public class RsiPostItem
    {
        public string Identifier { get; set; }
        public ParentEvent ParentEvent { get; set; }
    }

    public class ParentEvent
    {
        public string EventId { get; set; }
        public string CreationTime { get; set; }
        public string CollectionCode { get; set; }
        public string Author { get; set; }
        public string EventName { get; set; }
        public string Title { get; set; }
        public string Identifier { get; set; }
    }
}
