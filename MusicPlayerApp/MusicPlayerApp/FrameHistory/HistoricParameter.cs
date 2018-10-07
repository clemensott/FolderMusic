namespace FolderMusic.FrameHistory
{
    class HistoricParameter
    {
        public object Value { get; set; }

        public bool UseDataContext { get; set; }

        public object DataContext { get; set; }

        public HistoricParameter()
        {
        }

        public HistoricParameter(object value)
        {
            Value = value;
            UseDataContext = false;
            DataContext = null;
        }

        public HistoricParameter(object value, bool useDataContext)
        {
            Value = value;
            UseDataContext = useDataContext;
        }

        public HistoricParameter(object value, object dataContext)
        {
            Value = value;
            UseDataContext = true;
            DataContext = dataContext;
        }
    }
}
