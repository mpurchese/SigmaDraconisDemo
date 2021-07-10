namespace SigmaDraconis.CheckList
{
    public class Item
    {
        public int Id { get; set; }
        public bool IsStarted { get; set; }
        public bool IsComplete { get; set; }
        public bool IsRead { get; set; }
        public string Title { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string IconName { get; set; } = "None";
        public bool IsOptional { get; set; }

        public Item(int id)
        {
            this.Id = id;
        }
    }
}
