public class LocalStringDto
{
    public ErrorsDto Error;
    public AiInspectorDto AiInspector;

    public static LocalStringDto GetDefault()
    {
        return new LocalStringDto
        {
            Error = new ErrorsDto(),
            AiInspector = new AiInspectorDto()
        };
    }

    public class ErrorsDto
    {
        public string PlayerSettingsNotLoaded = "Warning. Unable to load PlayerSettings at: \"{0}\". Generating default file.";
        public string AiFileNotExists = "Unable to load AI. File does not exist at: \"{0}\"";
        public string AiErrorLoading = "Unable to load AI from: \"{0}\". ({1})";
        public string AiErrorSaving = "Unable to save AI to: \"{0}\". ({1})";
    }

    public class AiInspectorDto
    {
        public string Heading = "AI Inspector";
        public string DecisionLabel = "Decision:";
    }
}