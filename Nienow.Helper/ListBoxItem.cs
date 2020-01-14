namespace Nienow.Helper
{
    public class ListBoxItem
    {
        #region Constructors

        public ListBoxItem(string text, object tag)
        {
            this.Text = text;
            this.Tag = tag;
        }

        #endregion

        #region Attributes and Properties

        public string Text { get; set; }
        public object Tag { get; set; }

        #endregion
        
        #region Overriden Methods

        public override string ToString()
        {
            return this.Text;
        }

        #endregion
    }
}
