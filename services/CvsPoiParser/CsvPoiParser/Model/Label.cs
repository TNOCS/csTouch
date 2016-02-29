namespace PoiConvertor.Model
{
    public class Label
    {
        private int poisCounter;

        public Label(string name)
        {
            Name = name;
            Count = 1;
        }

        public string Name { get; set; }

        public int Count { get; set; }

        public bool IsEverywhere
        {
            get { return Count >= poisCounter; }
        }

        public void SetPoisCounter(int count)
        {
            poisCounter = count;
        }
    }
}