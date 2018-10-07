namespace FolderMusic.FrameHistory
{
    class Parameter
    {
        public object Value { get; private set; }

        public bool UseDataContext { get; private set; }

        public object DataContext { get; private set; }

        public Parameter(object value)
        {
            Value = value;
            UseDataContext = false;
            DataContext = null;
        }
        public Parameter(object value, object dataContext)
        {
            Value = value;
            UseDataContext = true;
            DataContext = dataContext;
        }
    }
}
