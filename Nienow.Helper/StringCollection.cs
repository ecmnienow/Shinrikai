using System.Collections.ObjectModel;

namespace Nienow.Helper
{
    public class StringCollection : ObservableCollection<string>
    {
        #region Constructors

        public StringCollection() { }

        #endregion

        #region Public Methods

        public StringCollection Clone()
        {
            StringCollection clonedCollection = new StringCollection();

            foreach (string item in this)
                clonedCollection.Add(item);

            return clonedCollection;
        }

        #endregion

        public string GetFirst()
        {
            foreach(string item in this)
                return item;

            return null;
        }
    }
}
