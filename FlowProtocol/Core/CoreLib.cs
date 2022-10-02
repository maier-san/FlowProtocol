namespace FlowProtocol.Core
{
    // Statishe Hilfsfunktionen
    public static class CoreLib
    {
        // Wendet eine Textoperation auf eine ganze Liste von Strings an.
        public static List<string> ApplyTextOperationToList(List<string> currentlist, Func<string, string> conv)
        {
            if (!currentlist.Any()) return currentlist;
            List<string> newlist = new List<string>();
            foreach (var s in currentlist)
            {
                newlist.Add(conv(s));
            }
            currentlist.Clear();
            return newlist;
        }

    }
}