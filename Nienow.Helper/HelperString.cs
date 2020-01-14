namespace Nienow.Helper
{
    public class HelperString
    {
        private HelperString()
        {
        }

        private static HelperString _instance;
        public static HelperString Instance
        {
            get
            {
                if (HelperString._instance == null)
                    HelperString._instance = new HelperString();

                return HelperString._instance;
            }
        }

        public string GetBetweenStrings(string text, string first, string second)
        {
            int indexFirst = text.IndexOf(first) + first.Length;
            int indexSecond = text.IndexOf(second, indexFirst);

            string requiredText = text.Substring(indexFirst, indexSecond - indexFirst);


            if (requiredText[requiredText.Length - 1] == ' ')
                requiredText = requiredText.Remove(requiredText.Length - 1);

            return requiredText;
        }
    }
}
