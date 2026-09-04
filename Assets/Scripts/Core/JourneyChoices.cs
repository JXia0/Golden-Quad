namespace LetGo
{
    public static class JourneyChoices
    {
        public static string KindergartenFirst { get; private set; }
        public static string StageStyle { get; private set; }
        public static string ResearchFirst { get; private set; }

        public static void Reset()
        {
            KindergartenFirst = string.Empty;
            StageStyle = string.Empty;
            ResearchFirst = string.Empty;
        }

        public static void Record(string category, string value, bool onlyIfEmpty)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            switch (category)
            {
                case "kindergarten":
                    if (!onlyIfEmpty || string.IsNullOrEmpty(KindergartenFirst)) KindergartenFirst = value;
                    break;
                case "stage":
                    StageStyle = value;
                    break;
                case "research":
                    if (!onlyIfEmpty || string.IsNullOrEmpty(ResearchFirst)) ResearchFirst = value;
                    break;
            }
        }

        public static string Get(string category)
        {
            return category switch
            {
                "kindergarten" => string.IsNullOrEmpty(KindergartenFirst) ? "我选择先迈出一步。" : KindergartenFirst,
                "stage" => string.IsNullOrEmpty(StageStyle) ? "我选择继续说下去。" : StageStyle,
                "research" => string.IsNullOrEmpty(ResearchFirst) ? "我选择从问题开始。" : ResearchFirst,
                _ => string.Empty
            };
        }
    }
}
